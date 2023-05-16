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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WagemasterEvents
{
    public partial class MainWindow : Window
    {
        private bool showDismissed = false;
        private ObservableCollection<Event> events;
        private System.Timers.Timer apiFetchTimer;
        private Event selectedEvent;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            DatabaseHelper.InitializeDatabase();
            LoadEvents();
                  

            ShowWindowCommand = new RelayCommand(ShowWindow);

            apiFetchTimer = new System.Timers.Timer();
            apiFetchTimer.Elapsed += ApiFetchTimer_Elapsed;
            apiFetchTimer.Interval = SettingsRepository.GetSettings().CacheTime * 1000 * 60;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
            var loadedEvents = EventsRepository.GetEvents(showDismissed);

            Application.Current.Dispatcher.Invoke(() =>
            {
                events = new ObservableCollection<Event>(loadedEvents);
                DataContext = new MainWindowViewModel { Events = events };
                var viewModel = (MainWindowViewModel)DataContext;
                viewModel.SelectedEvent = events.FirstOrDefault();
            });

            ListBox listBox = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                listBox = (ListBox)FindName("EventsListBox");
                listBox.ItemContainerGenerator.StatusChanged += ListBoxItemContainerGenerator_StatusChanged;
            });

            
        }
        private void ListBoxItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            var generator = (ItemContainerGenerator)sender;
            if (generator.Status == GeneratorStatus.ContainersGenerated)
            {
                foreach (var item in generator.Items)
                {
                    var listBoxItem = (ListBoxItem)generator.ContainerFromItem(item);
                    listBoxItem.LostFocus += ListBoxItem_LostFocus;
                }
            }
        }

        private void ListBoxItem_LostFocus(object sender, RoutedEventArgs e)
        {
            var listBoxItem = (ListBoxItem)sender;
            if (listBoxItem.DataContext is Event selectedEvent)
            {
                var viewModel = (MainWindowViewModel)DataContext;
                viewModel.SelectedEvent = selectedEvent;
            }
        }


        
        private async void ApiFetchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Fetch new events from the API
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server);

            // Save new events to the database
            EventsRepository.SaveEvents(fetchedEvents);

            // Load events from the database into the view model
            var loadedEvents = EventsRepository.GetEvents(showDismissed);

            // Update the events collection in the MainWindow class on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                events = new ObservableCollection<Event>(loadedEvents);
                DataContext = new MainWindowViewModel { Events = events };

                // Refresh the ListBox with the updated events
                EventsListBox.ItemsSource = events;
            });

            // Check for new events that need to be notified
            var now = DateTime.Now;
            var notifiedEvents = fetchedEvents.Where(eventItem => eventItem.NextReminderDate <= now).ToList();

            if (notifiedEvents.Count > 0)
            {
                MessageBox.Show($"There are {notifiedEvents.Count} tasks due");
            }
        }

        private void ToggleDismissedButton_Click(object sender, RoutedEventArgs e)
        {
            showDismissed = !showDismissed;
            events = new ObservableCollection<Event>(EventsRepository.GetEvents(showDismissed));
            LoadEvents();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (MainWindowViewModel)DataContext;
            var selectedEvent = viewModel.SelectedEvent;

            if (selectedEvent != null)
            {
                EventsRepository.UpdateEvent(selectedEvent);
                LoadEvents(); // Refresh the events after updating

                // Set the last selected event as the selected event again
                //var viewModel = (MainWindowViewModel)DataContext;
                viewModel.SelectedEvent = selectedEvent;
            }
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
