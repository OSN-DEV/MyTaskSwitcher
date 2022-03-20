using OsnCsLib.Common;
using OsnCsLib.WPFComponent.Control;
using System.Timers;
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
        private string _inputNum = "";
        private Timer _timer = null;
        #endregion

        #region Constructor
        public TaskGridWindow() {
            InitializeComponent();
            this._timer = new Timer();
            this._timer.Interval = 1000;
            this._timer.Enabled = false;
            this._timer.Elapsed += _timer_Elapsed;
        }
        #endregion

        #region Event
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _timer_Elapsed(object sender, ElapsedEventArgs e) {
            this._timer.Enabled = false;
            this._inputNum = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (!this._viewModel.IsSearchMode && Key.D0 <= e.Key && e.Key <= Key.D9) {
                e.Handled = true;
                if (!this._timer.Enabled) {
                    this._inputNum = "";
                    this._timer.Enabled = true;
                }
                this._inputNum += (e.Key - Key.D0).ToString();
                if (2 == this._inputNum.Length) {
                    this._timer.Enabled = false;
                    var index = int.Parse(this._inputNum);
                    if (index < this._viewModel.ItemList.Count) {
                        this.SetListViewFocus(index);
                    }
                }
            }

            switch(e.Key) {
                case Key.F:
                    if (Util.IsModifierPressed(ModifierKeys.Control)) {
                        e.Handled = true;
                        this._viewModel.IsSearchMode = !this._viewModel.IsSearchMode;
                        if (this._viewModel.IsSearchMode) {
                            this.cSearchText.Focus();
                        } else {
                            this.SetListViewFocus(this.cTaskList.SelectedIndex);
                        }
                    }
                    break;
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
                    var selectedIndex = this.cTaskList.SelectedIndex;
                    this._viewModel.CloseApp(this.cTaskList.SelectedIndex);
                    this._viewModel.GetTasks();
                    selectedIndex--;
                    if (selectedIndex < 0) {
                        selectedIndex = 0;
                    }
                    this.SetListViewFocus(selectedIndex);
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
        private void SetListViewFocus(int index = 0) {
            if (0 == this.cTaskList.Items.Count) {
                return;
            }
            this.cTaskList.SelectedIndex = index;
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
