using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyTaskSwitcher.Data {
    public class TaskItem {

        #region Public Property
        /// <summary>
        /// icon
        /// </summary>
        public BitmapSource Icon { set; get; } = null;

        /// <summary>
        /// index
        /// </summary>
        public int Index { set; get; } = 0;

        /// <summary>
        /// title
        /// </summary>
        public string Title { set; get; } = "";

        /// <summary>
        /// window handle
        /// </summary>
        public IntPtr Handle { set; get; }

        /// <summary>
        /// process
        /// </summary>
        public Process AppProcess { set; get; }

        /// <summary>
        /// process
        /// </summary>
        public int AppProcessId { set; get; }


        /// <summary>
        /// 
        /// </summary>
        public string SortKey { set; get; }
        #endregion

        #region  Constructor
        public TaskItem() { }

        public TaskItem(IntPtr Handle, string Title) {
            this.Handle = Handle;
            this.Title = Title;
        }

        public TaskItem(int Index, IntPtr Handle, string Title) {
            this.Index = Index;
            this.Handle = Handle;
            this.Title = Title;
        }
        #endregion
    }
}
