using MyTaskSwitcher.AppCommon;
using MyTaskSwitcher.UI.TaskGrid;
using OsnCsLib.Common;
using System.Windows;

namespace MyTaskSwitcher {
    /// <summary>
    /// スタートアップ
    /// </summary>
    public partial class App : Application {
        #region Declaration
        private AppLaunchChecker _launchChecker = new AppLaunchChecker("MyProgrammableTenkey");
        #endregion

        #region Event
        /// <summary>
        /// System.Windows.Application.Startup
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            if (this._launchChecker.IsLaunched()) {
                this.Shutdown();
                return;
            }

            if (!System.IO.Directory.Exists(Constant.IconCache)) {
                System.IO.Directory.CreateDirectory(Constant.IconCache);
            }

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var window = new TaskGridWindow();
            window.Show();
        }

        /// <summary>
        /// System.Windows.Application.Exit
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);
        }
        #endregion
    }
}
