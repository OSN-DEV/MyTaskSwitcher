using MyTaskSwitcher.AppCommon;
using MyTaskSwitcher.Data;
using MyTaskSwitcher.Func;
using OsnCsLib.WPFComponent.Bind;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;


namespace MyTaskSwitcher.UI.TaskGrid {
    public class TaskGridViewModel : BaseBindable {

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
        /// get task list
        /// </summary>
        private void GetTasks() {
            this._itemList.Clear();
            this._index = 0;
            Process[] processes = Process.GetProcesses();
            foreach (var p in processes) {
                if (0 < p.MainWindowTitle.Length) {
                    if (p.MainWindowTitle.StartsWith("MyTaskSwither")) {
                        continue;
                    }

                    var iconFile = $"{Constant.IconCache}\\{p.ProcessName}";
                    if (!File.Exists(iconFile)) {
                        var icon = GetAppIcon(p.MainWindowHandle);

                        if (File.Exists(iconFile)) {
                            File.Delete(iconFile);
                        }
                        if (null == icon) {
                            continue;
                        }
                        icon.ToBitmap().Save(iconFile, ImageFormat.Png);
                    }
                    var item = new TaskItem(p.MainWindowHandle, p.MainWindowTitle);
                    item.Icon = this.GetBitmapSource(iconFile);
                    this._itemList.Add(item);
                }
            }

            var rest = this._itemList.Count % ItemCountPerPage;
            if (0 < rest) {
                rest = ItemCountPerPage - rest;
            }
            for (int i = 0; i < rest; i++) {
                this._itemList.Add(new TaskItem());
            }
            this._maxIndex = this._itemList.Count / ItemCountPerPage - 1;
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
                //if (IntPtr.Zero == item.Handle) {
                //    break;
                //}
                this.ItemList[i] = this._itemList[i];
            }
            this.PageData = $"{index + 1}/{this._maxIndex + 1}";
  //          base.SetProperty(nameof(ItemList));
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
