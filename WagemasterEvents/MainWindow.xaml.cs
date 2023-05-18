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
using System.ComponentModel;

namespace WagemasterEvents
{
    public partial class MainWindow : Window
    {
        private bool showDismissed = false;
        private ObservableCollection<Event> events;
        private System.Timers.Timer apiFetchTimer;
        private Event selectedEvent;
        private WindowState previousWindowState;
        private bool minimizeToTray = true;

        public MainWindow()
        {
            InitializeComponent();

            // Register the SizeChanged event handler
            SizeChanged += MainWindow_SizeChanged;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            DataContext = new MainWindowViewModel();

            DatabaseHelper.InitializeDatabase();
            LoadEvents();
                  

            ShowWindowCommand = new RelayCommand(ShowWindow);

            apiFetchTimer = new System.Timers.Timer();
            apiFetchTimer.Elapsed += ApiFetchTimer_Elapsed;
            apiFetchTimer.Interval = SettingsRepository.GetSettings().CacheTime * 1000 * 60;

            Loaded += MainWindow_Loaded;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            EventsRepository.DeleteEvents(selectedEvent);

            LoadEvents();
            MessageBox.Show("Reminders list reset successfully.");
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Store the previous WindowState
                previousWindowState = WindowState;
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Store the previous WindowState
                previousWindowState = WindowState;
            }
            else if (previousWindowState == WindowState.Minimized && WindowState == WindowState.Normal)
            {
                // Maximize the MainWindow
                WindowState = WindowState.Maximized;
            }
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

            // Load events from the database into the view model, including the dismissed events
            var loadedEvents = EventsRepository.GetEvents(false); // Include dismissed events

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
            var notifiedEvents = loadedEvents.Where(eventItem => eventItem.NextReminderDate <= now && !eventItem.Dismissed).ToList();

            if (notifiedEvents.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show($"There are {notifiedEvents.Count} tasks due");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowWindowCommand.Execute(null);

                    if (WindowState == WindowState.Minimized)
                    {
                        // Restore the MainWindow if it was previously maximized
                        if (previousWindowState == WindowState.Maximized)
                        {
                            ShowWindowCommand.Execute(null); // Invoke the ShowWindowCommand
                        }
                        else
                        {
                            ShowWindowCommand.Execute(null); // Invoke the ShowWindowCommand
                            WindowState = WindowState.Normal; // Set the WindowState to Normal
                        }
                    }
                });
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
                viewModel.SelectedEvent = selectedEvent;
            }
            MessageBox.Show("Reminder saved successfully.");
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

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (minimizeToTray)
            {
                e.Cancel = true; // Cancel the closing event
                Hide(); // Hide the window instead of closing
            }
        }


    }
}
