using MyTaskSwitcher.Data;
using MyTaskSwitcher.Func;
using OsnCsLib.WPFComponent.Bind;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

        //[DllImport("user32.dll")]
        //public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
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
        private int _index = 0;
        private int _maxIndex = 0;
        private const int ItemCountPerPage = 12;
        private List<TaskItem> _itemList = new List<TaskItem>();

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

        /// <summary>
        /// previous page click
        /// </summary>
        public DelegateCommand PrevPageClickCommand { set; get; }

        /// <summary>
        /// next page click
        /// </summary>
        public DelegateCommand NextPageClickCommand { set; get; }

        /// <summary>
        /// page data
        /// </summary>
        private string _pageData;
        public string PageData {
            set { base.SetProperty(ref this._pageData, value); }
            get { return this._pageData; }
        }
        #endregion

        #region Constructor
        public TaskGridViewModel() {

            this.ItemList = new ObservableCollection<TaskItem>();
            for (int i = 0; i < ItemCountPerPage; i++) {
                this.ItemList.Add(new TaskItem());
            }
            this.TaskItemClickCommand = new DelegateCommandWithParam<int>(TaskItemClick);
            this.PrevPageClickCommand = new DelegateCommand(PreviousPageClick);
            this.NextPageClickCommand = new DelegateCommand(NextPageClick);
        }
        #endregion

        #region Public Method
        /// <summary>
        /// refresh task list
        /// </summary>
        public void Refresh() {
            this.GetTasks();
            this.ShowPage(0);
        }

        /// <summary>
        /// show previous page
        /// </summary>
        public void PreviousPageClick() {
            this._index--;
            if (this._index < 0) {
                this._index = this._maxIndex;
            }
            this.ShowPage(this._index);
        }

        /// <summary>
        /// show next page
        /// </summary>
        public void NextPageClick() {
            this._index++;
            if (this._maxIndex < this._index) {
                this._index = 0;
            }
            this.ShowPage(this._index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Key"></param>
        public void TaskItemPressed(string Key) {
            var keys = new List<string> { "Q", "W", "E", "R", "A", "S", "D", "F","Z", "X","C", "V" };
            var index =  keys.IndexOf(Key);
            this.TaskItemClick(index);
        }
        #endregion

        #region Private Method
        /// <summary>
        /// 
        /// </summary>
        private void GetTasks() {
            this._itemList.Clear();
            this._index = 0;

            EnumWindows(new EnumWindowsDelegate(EnumWindowCallBack), IntPtr.Zero);

            var rest = this._itemList.Count % ItemCountPerPage;
            if (0 < rest) {
                rest = ItemCountPerPage - rest;
            }
            for (int i = 0; i < rest; i++) {
                this._itemList.Add(new TaskItem());
            }
            this._maxIndex = this._itemList.Count / ItemCountPerPage - 1;
            this.ShowPage(0);
        }


        private bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam) {

            int textLen = GetWindowTextLength(hWnd);
            if (0 == textLen) {
                return true;
            }

            bool hasOwner = ((IntPtr)0 != GetWindow(hWnd, GW_OWNER));
            bool isChild = ((IntPtr)0 != GetParent(hWnd));
            if (IsWindowAvailable(hWnd) && !hasOwner && !isChild) {
                int processID;
                Icon icon = null;
                Process process;

                
                GetWindowThreadProcessId(hWnd, out processID);              // ウィンドウハンドル→プロセスID
                if (processID == Process.GetCurrentProcess().Id) {
                    // 自身は除外
                    return true;
                }
                
                process = Process.GetProcessById(processID);        // プロセスID→プロセス

                // プロセス→実行ファイル名→アイコン
                try {
                    icon = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }

                StringBuilder tsb = new StringBuilder(textLen + 1);
                GetWindowText(hWnd, tsb, tsb.Capacity);
                var item = new TaskItem(hWnd, tsb.ToString());
                if (null != icon) {
                    item.Icon = ConverToSource(icon.ToBitmap());
                }
                this._itemList.Add(item);
            }


            //すべてのウィンドウを列挙する
            return true;
        }

        //列挙対象のウインドウである
        bool IsWindowAvailable(IntPtr hWnd) {
            RECT rc = new RECT();
            GetWindowRect(hWnd, out rc);

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





        ////あの手この手でアイコンを取得[小さい]
        //HICON GetSmallIconFromWindow(HWND hWnd) {
        //    HICON hIcon = NULL;

        //    if (hWnd == NULL || IsHungAppWindow(hWnd)) return NULL;

        //    if (!SendMessageTimeout(hWnd, WM_GETICON, ICON_SMALL, 0, SMTO_ABORTIFHUNG, 500, (PDWORD_PTR) & hIcon)) {
        //        DWORD dwError = GetLastError();

        //        if (dwError == ERROR_SUCCESS || dwError == ERROR_TIMEOUT) {
        //            return LoadIcon(NULL, IDI_APPLICATION);
        //        }
        //    }
        //    if (hIcon == NULL) {
        //        hIcon = (HICON)GetClassLongPtr(hWnd, GCLP_HICONSM);
        //    }
        //    if (hIcon == NULL) {
        //        HWND hParent = GetParent(hWnd);
        //        if (IsWindowVisible(hParent)) return GetSmallIconFromWindow(hParent);
        //    }
        //    return hIcon;
        //}

        ////あの手この手でアイコンを取得[大きい]
        //HICON GetLargeIconFromWindow(HWND hWnd) {
        //    HICON hIcon = NULL;

        //    if (hWnd == NULL || IsHungAppWindow(hWnd)) return NULL;

        //    if (!SendMessageTimeout(hWnd, WM_GETICON, ICON_BIG, 0, SMTO_ABORTIFHUNG, 500, (PDWORD_PTR) & hIcon)) {
        //        DWORD dwError = GetLastError();

        //        if (dwError == ERROR_SUCCESS || dwError == ERROR_TIMEOUT) {
        //            return LoadIcon(NULL, IDI_APPLICATION);
        //        }
        //    }
        //    if (hIcon == NULL) {
        //        hIcon = (HICON)GetClassLongPtr(hWnd, GCLP_HICON);
        //    }
        //    if (hIcon == NULL) {
        //        HWND hParent = GetParent(hWnd);
        //        if (IsWindowVisible(hParent)) return GetLargeIconFromWindow(hParent);
        //    }
        //    return hIcon;
        //}

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
        /// show data
        /// </summary>
        /// <param name="index"></param>
        private void ShowPage(int index) {
            // this.ItemList.Clear();

            var offset = index * ItemCountPerPage;
            for (int i = 0; i < ItemCountPerPage; i++) {
                var item = this._itemList[i + offset];
                item.Index = i;
                this.ItemList[i] = this._itemList[i];
            }
            this.PageData = $"{index + 1}/{this._maxIndex + 1}";
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
        /// get app icon
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        private Icon GetAppIcon(IntPtr hwnd) {
            IntPtr iconHandle = Win32APIs.SendMessage(hwnd, Win32APIs.WM_GETICON, Win32APIs.ICON_SMALL2, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = Win32APIs.SendMessage(hwnd, Win32APIs.WM_GETICON, Win32APIs.ICON_SMALL, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = Win32APIs.SendMessage(hwnd, Win32APIs.WM_GETICON, Win32APIs.ICON_BIG, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = Win32APIs.GetClassLongPtr(hwnd, Win32APIs.GCL_HICON);
            if (iconHandle == IntPtr.Zero)
                iconHandle = Win32APIs.GetClassLongPtr(hwnd, Win32APIs.GCL_HICONSM);

            if (iconHandle == IntPtr.Zero)
                return null;

            Icon icn = Icon.FromHandle(iconHandle);

            return icn;
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

        /// get BitmapSource from file path
        /// </summary>
        /// <param name="data">image data</param>
        /// <returns>BitmapSource</returns>
        private BitmapSource GetBitmapSource(string file) {
            BitmapSource bitmapSource = null;
            try {
                byte[] data;
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (var memStream = new MemoryStream()) {
                    stream.CopyTo(memStream);
                    data = memStream.GetBuffer();
                }

                using (var stream = new MemoryStream(data)) {
                    var bitmapDecoder = BitmapDecoder.Create(
                                        stream,
                                        BitmapCreateOptions.PreservePixelFormat,
                                        BitmapCacheOption.OnLoad);
                    // var writable = new WriteableBitmap(bitmapDecoder.Frames.Single());
                    var writable = new WriteableBitmap(bitmapDecoder.Frames.First());
                    writable.Freeze();
                    bitmapSource = (BitmapSource)writable;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return bitmapSource;
        }
        #endregion
    }
}
