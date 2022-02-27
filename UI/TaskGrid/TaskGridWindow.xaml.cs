using OsnCsLib.Common;
using OsnCsLib.WPFComponent.Control;
using System.Windows;
using System.Windows.Input;

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
                    break;
                //case Key.J:
                //    e.Handled = true;
                //    this._viewModel.PreviousPageClick();
                //    break;
                //case Key.K:
                //    e.Handled = true;
                //    this._viewModel.NextPageClick();
                //    break;
                //case Key.Q:
                //case Key.W:
                //case Key.E:
                //case Key.R:
                //case Key.A:
                //case Key.S:
                //case Key.D:
                //case Key.F:
                //case Key.Z:
                //case Key.X:
                //case Key.C:
                //case Key.V:
                //    e.Handled = true;
                //    this._viewModel.TaskItemPressed(e.Key.ToString().ToUpper());
                //    break;
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
            };
            this.Closing += (sender, e) => {
                this.ExitApp();
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
            this._viewModel.Refresh();
            base.OnHotkeyPressed();

        }
        #endregion

        #region Private Method
        /// <summary>
        /// show task list view
        /// </summary>
        private void ShowTaskList() {
            this._viewModel.Refresh();
            base.SetWindowsState(false);
            
        }

        /// <summary>
        /// exit app
        /// </summary>
        private void ExitApp() {
            Application.Current.Shutdown();
        }
        #endregion
    }
}
