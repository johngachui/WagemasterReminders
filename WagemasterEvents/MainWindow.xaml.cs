using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Timers;
using WagemasterEvents.Database;
using WagemasterEvents.Models;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using System.Windows.Input;

namespace WagemasterEvents
{
    public partial class MainWindow : Window
    {
        private bool showDismissed = false;
        private ObservableCollection<Event> events;
        private Notifier notifier;
        private System.Timers.Timer apiFetchTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            ShowWindowCommand = new RelayCommand(ShowWindow);
            DatabaseHelper.InitializeDatabase();
            LoadEvents();

            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: this,
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Dispatcher;
            });

            apiFetchTimer = new System.Timers.Timer();
            apiFetchTimer.Elapsed += ApiFetchTimer_Elapsed;
            apiFetchTimer.Interval = SettingsRepository.GetSettings().CacheTime * 1000;
            apiFetchTimer.Start();
        }

        private void LoadEvents()
        {
            // Load events from the database here
            events = new ObservableCollection<Event>(EventsRepository.GetEvents(showDismissed));
            EventsDataGrid.ItemsSource = events;

            var now = DateTime.Now;
            foreach (var eventItem in events)
            {
                if (eventItem.NextReminderDate <= now)
                {
                    notifier.ShowInformation($"Reminder: {eventItem.Reminder}");
                }
            }
        }

        // Other methods, including ToggleDismissedButton_Click, SaveButton_Click, and SettingsMenuItem_Click go here

        private async void ApiFetchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Fetch new events from the API
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server);
            EventsRepository.SaveEvents(fetchedEvents);

            // Refresh the events list
            LoadEvents();
        }

        public ICommand ShowWindowCommand { get; }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }
    }
}
