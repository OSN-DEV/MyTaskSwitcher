using OsnCsLib.Common;
using OsnCsLib.WPFComponent.Control;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MyTaskSwitcher.UI.TaskGrid {
    /// <summary>
    /// TaskGridWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TaskGridWindow : ResidentWindow {

        #region Declaration
        private TaskGridViewModel _viewModel;
        #endregion

        #region Constructor
        public TaskGridWindow() {
            InitializeComponent();
        }
        #endregion

        #region Event
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            switch(e.Key) {
                case Key.Escape:
                    e.Handled = true;
                    base.SetWindowsState(true);
                    break;
                case Key.Enter:
                    e.Handled = true;
                    this._viewModel.TaskItemSelected(this.cTaskList.SelectedIndex);
                    break;
                case Key.Delete:
                    e.Handled = true;
                    this._viewModel.CloseApp(this.cTaskList.SelectedIndex);
                    this._viewModel.GetTasks();
                    this.SetListViewFocus();
                    break;
            }
        }

        void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            this._viewModel.TaskItemSelected(this.cTaskList.SelectedIndex);
        }

        private void ResidentWindow_StateChanged(object sender, System.EventArgs e) {
            if (this.WindowState == WindowState.Normal) {
                SetListViewFocus();
            }
        }
        #endregion

        #region Override Method
        /// <summary>
        /// 
        /// </summary>
        protected override void SetUp() {
            //
            var appName = Util.GetVersion(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //
            base.SetUpHotKey(ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt, Key.X);
            base.SetupNofityIcon(appName, new System.Drawing.Icon("app.ico"));

            // create a context menu
            base.AddContextMenu("Show", (sender, e) => this.ShowTaskList());
            base.AddContextMenuSeparator();
            base.AddContextMenu("Exit", (sender, e) => this.ExitApp());

            // event
            this.Loaded += (sender, e) => {
                this.Title = appName;
                this.SetListViewFocus();
                this.SetWindowsState(true);
                // this.SizeToContent = SizeToContent.Height;
            };
            this.Closing += (sender, e) => {
                this.ExitApp();
            };
            this.ContentRendered += (s, e) => {
                System.Diagnostics.Debug.WriteLine("ContentRendered");
            };
            bool ignoreActivate = true;

            this.Activated += (sender, e) => {
                if (ignoreActivate) {
                    ignoreActivate = false;
                    return;
                }
                this._viewModel.GetTasks();
                this.SetListViewFocus();
                base.OnHotkeyPressed();
                
            };

            // set view model
            this._viewModel = new TaskGridViewModel();
            this._viewModel.TaskItemClickCallback += (sender, e) => {
                base.SetWindowsState(true);
            };
            this.DataContext = this._viewModel;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnHotkeyPressed() {
            //this._viewModel.GetTasks();
            //this.SetListViewFocus();
            base.OnHotkeyPressed();

        }
        #endregion

        #region Private Method
        /// <summary>
        /// show task list view
        /// </summary>
        private void ShowTaskList() {
            base.SetWindowsState(false);
        }

        /// <summary>
        /// exit app
        /// </summary>
        private void ExitApp() {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetListViewFocus() {
            if (0 == this.cTaskList.Items.Count) {
                return;
            }
            this.cTaskList.SelectedIndex = 0;
            this.cTaskList.Focus();
            DoEvents();
            var item = (ListViewItem)(this.cTaskList.ItemContainerGenerator.ContainerFromItem(cTaskList.SelectedItem));
            if (null != item) {
                item.Focus();
                DoEvents();
            }

        }

        private void DoEvents() {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(ExitFrames);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
        private object ExitFrames(object obj) {
            ((DispatcherFrame)obj).Continue = false;
            return null;
        }
        #endregion


    }
}
