using MyTaskSwitcher.Data;
using MyTaskSwitcher.Func;
using OsnCsLib.WPFComponent.Bind;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyTaskSwitcher.UI.TaskGrid {
    public class TaskGridViewModel : BaseBindable {

        #region Win32 API
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("dwmapi.dll")]
        private static extern long DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out IntPtr pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, int wCmd);

        [DllImport("User32.Dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO {
            public int cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public int dwStyle;
            public int dwExStyle;
            public int dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public short atomWindowType;
            public short wCreatorVersion;
        }

        public enum DWMWINDOWATTRIBUTE {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,//ウィンドウのRect
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,      // EnumWindowsで見えないUWPアプリを除外
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        };

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const long WS_EX_NOREDIRECTIONBITMAP = 0x00200000L;
        private const int GW_OWNER = 4;
        #endregion

        #region Declaration

        /// <summary>
        /// task item click event
        /// </summary>
        private EventHandler<EventArgs> OnTaskItemClickCallback;
        public event EventHandler<EventArgs> TaskItemClickCallback {
            add { OnTaskItemClickCallback += value; }
            remove { OnTaskItemClickCallback -= value; }
        }
        #endregion

        #region Public Property
        /// <summary>
        /// ItemList
        /// </summary>
        public ObservableCollection<TaskItem> ItemList { set; get; }

        /// <summary>
        /// select item command
        /// </summary>
        public DelegateCommandWithParam<int> TaskItemClickCommand { set; get; }
        #endregion

        #region Constructor
        public TaskGridViewModel() {
            this.ItemList = new ObservableCollection<TaskItem>();
            this.TaskItemClickCommand = new DelegateCommandWithParam<int>(TaskItemClick);
        }
        #endregion

        #region Public Method
        /// <summary>
        /// 
        /// </summary>
        public void GetTasks() {
            this.ItemList.Clear();
            EnumWindows(new EnumWindowsDelegate(EnumWindowCallBack), IntPtr.Zero);
            // Array.Sort(this.ItemList,);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Key"></param>
        public void TaskItemSelected(int index) {
            this.TaskItemClick(index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inde"></param>
        public void CloseApp(int index) {
            var process = ItemList[index].AppProcess;
            process.Refresh();
            if (process.HasExited) {
                process.CloseMainWindow();
                process.Close();
            }

            //this.ItemList[index].AppProcess.CloseMainWindow();
            //this.ItemList[index].AppProcess.Close();
        }

        #endregion

        #region Private Method
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lparam"></param>
        /// <returns></returns>
        private bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam) {

            int textLen = GetWindowTextLength(hWnd);
            if (0 == textLen) {
                return true;
            }

            bool hasOwner = ((IntPtr)0 != GetWindow(hWnd, GW_OWNER));
            bool isChild = ((IntPtr)0 != GetParent(hWnd));
            if (IsWindowAvailable(hWnd) && !hasOwner && !isChild) {
                Icon icon = null;
                
                GetWindowThreadProcessId(hWnd, out int processID);              // ウィンドウハンドル→プロセスID
                if (processID == Process.GetCurrentProcess().Id) {
                    // 自身は除外
                    return true;
                }

                StringBuilder tsb = new StringBuilder(textLen + 1);
                GetWindowText(hWnd, tsb, tsb.Capacity);
                var item = new TaskItem(hWnd, tsb.ToString());
                item.AppProcess = Process.GetProcessById(processID);        // プロセスID→プロセス
                item.SortKey = item.AppProcess.MainModule.FileName;
                // プロセス→実行ファイル名→アイコン
                try {
                    icon = System.Drawing.Icon.ExtractAssociatedIcon(item.AppProcess.MainModule.FileName);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
                if (null != icon) {
                    item.Icon = ConverToSource(icon.ToBitmap());
                }
                // this._itemList.Add(item);
                this.ItemList.Add(item);
            }


            //すべてのウィンドウを列挙する
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        bool IsWindowAvailable(IntPtr hWnd) {
            GetWindowRect(hWnd, out RECT rc);

            bool isVisible = ((long)GetWindowLongPtr(hWnd, GWL_STYLE) & WS_VISIBLE) != 0; // 可視状態
            bool isToolWindow = ((int)GetWindowLongPtr(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0; // ツールウインドウ

            if (isVisible && !isToolWindow && (0 != rc.right- rc.left) && (0 != rc.bottom - rc.top)) {
                bool isRendered = ((int)GetWindowLongPtr(hWnd, GWL_EXSTYLE) & WS_EX_NOREDIRECTIONBITMAP) != 0;
                if (isRendered) {
                    var result = DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out IntPtr isCloaked, Marshal.SizeOf(typeof(bool)));
                    if (result != 0) {
                        return false;
                    }
                    if ((IntPtr)0 != isCloaked) {
                        return false;
                    }
                    return true;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        private long GetWindowLongPtr(IntPtr hWnd, int nIndex) {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex).ToInt64();
            else
                return GetWindowLongPtr32(hWnd, nIndex).ToInt32();
        }

        /// <summary>
        /// task item click
        /// </summary>
        /// <param name="index"></param>
        private void TaskItemClick(int index) {
            Win32APIs.SetForegroundWindow(this.ItemList[index].Handle);
            this.OnTaskItemClickCallback?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Convert bitmap to bitmapsource
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private BitmapSource ConverToSource(Bitmap bitmap) {
            System.Windows.Media.Imaging.BitmapSource bitmapSource = null;
            using (var ms = new System.IO.MemoryStream()) {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                bitmapSource =
                    BitmapFrame.Create(
                        ms,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.OnLoad
                    );
            }
            return bitmapSource;
        }
        #endregion
    }
}
