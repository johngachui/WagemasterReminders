using System.Windows;
using System.Windows.Forms;
using WagemasterEvents.Database;

namespace WagemasterEvents
{
    public partial class App : System.Windows.Application
    {
        private NotifyIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Hide();

            notifyIcon = new NotifyIcon();
            notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            notifyIcon.Icon = new System.Drawing.Icon("icon.ico");
            notifyIcon.Visible = true;

            CreateContextMenu();

            LoadEventsFromApi();
        }

        private async void LoadEventsFromApi()
        {
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;
            var username = SettingsRepository.GetSettings().Username;
            var password = SettingsRepository.GetSettings().Password;
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server,username,password);
            EventsRepository.SaveEvents(fetchedEvents);
        }

        private void CreateContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Show").Click += (s, e) => ShowMainWindow();
            notifyIcon.ContextMenuStrip.Items.Add("Exit").Click += (s, e) => ExitApplication();
        }

        private void ShowMainWindow()
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
        }

        private void ExitApplication()
        {
            notifyIcon.Visible = false;
            Shutdown();
        }
    }
}
