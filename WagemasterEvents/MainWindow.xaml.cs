using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Timers;
using WagemasterEvents.Database;
using WagemasterEvents.Models;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;

namespace WagemasterEvents
{
    public partial class MainWindow : Window
    {
        private bool showDismissed = false;
        private ObservableCollection<Event> events;
        private System.Timers.Timer apiFetchTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            DatabaseHelper.InitializeDatabase();
            LoadEvents();
                  

            ShowWindowCommand = new RelayCommand(ShowWindow);

            apiFetchTimer = new System.Timers.Timer();
            apiFetchTimer.Elapsed += ApiFetchTimer_Elapsed;
            apiFetchTimer.Interval = SettingsRepository.GetSettings().CacheTime * 1000;
            apiFetchTimer.Start();
        }

        private bool eventsLoaded = false;
        public ObservableCollection<Event> Events
        {
            get { return events; }
            set { events = value; }
        }

        private async void LoadEvents()
        {
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;

            // Fetch new events from the API and save them to the database
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server);
            EventsRepository.SaveEvents(fetchedEvents);

            // Load events from the database into the view model
            var events = EventsRepository.GetEvents(showDismissed);
            var viewModel = (MainWindowViewModel)DataContext;
            viewModel.Events = new ObservableCollection<Event>(events);
            viewModel.SelectedEvent = events.FirstOrDefault(); // Set the first event as the selected event
            
            Debug.WriteLine($"events count - {events.Count}");

            // Check for new events that need to be notified
            /*var now = DateTime.Now;
            var notifiedEvents = new List<Event>();
            foreach (var eventItem in events)
            {
                if (eventItem.NextReminderDate <= now && !notifiedEvents.Contains(eventItem))
                {
                    MessageBox.Show($"Reminder: {eventItem.Reminder}");
                    notifiedEvents.Add(eventItem);
                }
            }*/
        }





        private async void ApiFetchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Fetch new events from the API
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server);

            // Save new events to the database
            EventsRepository.SaveEvents(fetchedEvents);

            // Refresh the events list
            LoadEvents();

            // Check for new events that need to be notified
            var now = DateTime.Now;
            var notifiedEvents = new List<Event>();
            foreach (var eventItem in events)
            {
                if (eventItem.NextReminderDate <= now && !notifiedEvents.Contains(eventItem))
                {
                    MessageBox.Show($"Reminder: {eventItem.Reminder}");
                    notifiedEvents.Add(eventItem);
                }
            }
        }


        private void ToggleDismissedButton_Click(object sender, RoutedEventArgs e)
        {
            showDismissed = !showDismissed;
            events = new ObservableCollection<Event>(EventsRepository.GetEvents(showDismissed));
           
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save changes to the EventsList table
            EventsRepository.SaveEvents(events);
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Open the MainSettings.xaml window
            var settingsWindow = new MainSettings();
            settingsWindow.ShowDialog();
        }

        public ICommand ShowWindowCommand { get; }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }
    }
}
