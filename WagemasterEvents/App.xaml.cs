using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace WagemasterEvents
{
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            MainWindow = new MainWindow();
            MainWindow.Closed += MainWindow_Closed;
            MainWindow.Hide(); // Hide the main window initially
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            notifyIcon.Dispose();
        }
    }
}
