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
using System.Runtime.InteropServices;

namespace WagemasterEvents
{
    public partial class MainWindow : Window
    {
        private bool showDismissed = false;
        private ObservableCollection<Event>? events;
        private System.Timers.Timer apiFetchTimer;
        private Event? selectedEvent;
        private WindowState previousWindowState;
        private bool minimizeToTray = true;
        public event Action<int>? CacheTimeChanged;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        public MainWindow()
        {
            InitializeComponent();

            // Initialize the collection
            events = new ObservableCollection<Event>();

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

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset list - all changes will be lost?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                EventsRepository.DeleteEvents(selectedEvent);

                // Fetch new events from the API
                var apiHelper = new ApiHelper();
                var server = SettingsRepository.GetSettings().Server;
                var username = SettingsRepository.GetSettings().Username;
                var password = SettingsRepository.GetSettings().Password;
                var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server,username,password);

                // Save new events to the database
                EventsRepository.SaveEvents(fetchedEvents);

                // Load events from the database into the view model, including the dismissed events
                var loadedEvents = EventsRepository.GetEvents(showDismissed); // Include dismissed events

                // Update the events collection in the MainWindow class on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    events = new ObservableCollection<Event>(loadedEvents);
                    DataContext = new MainWindowViewModel { Events = events };

                    // Refresh the ListBox with the updated events
                    EventsListBox.ItemsSource = events;
                });

                // Update button text
                ToggleDismissedButton.Content = showDismissed ? "Hide Dismissed" : "Show Dismissed";

                MessageBox.Show("Reminders list reset successfully.");
            }
        }

        bool IsMessageBoxOpen(string caption)
        {
            return FindWindow(null, caption) != IntPtr.Zero;
        }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Store the previous WindowState
                previousWindowState = WindowState;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // Store the previous WindowState
                previousWindowState = WindowState;
            }
            else if (previousWindowState == WindowState.Minimized && WindowState == WindowState.Normal)
            {
                // Maximize the MainWindow
                WindowState = WindowState.Normal;
            }
        }


        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            apiFetchTimer.Start();
        }

        private bool eventsLoaded = false;
        public ObservableCollection<Event>? Events
        {
            get { return events; }
            set { events = value; }
        }

        private async void LoadEvents()
        {
            try
            {
                var apiHelper = new ApiHelper();
                var server = SettingsRepository.GetSettings().Server;
                var username = SettingsRepository.GetSettings().Username;
                var password = SettingsRepository.GetSettings().Password;
                // Fetch new events from the API and save them to the database
                var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server, username, password);
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
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }

        }
        private void ListBoxItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
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


        private async void ApiFetchTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Fetch new events from the API
            var apiHelper = new ApiHelper();
            var server = SettingsRepository.GetSettings().Server;
            var username = SettingsRepository.GetSettings().Username;
            var password = SettingsRepository.GetSettings().Password;
            var fetchedEvents = await apiHelper.FetchEventsFromApiAsync(server,username,password);

            // Save new events to the database
            EventsRepository.SaveEvents(fetchedEvents);

            // Load events from the database into the view model, including the dismissed events
            var loadedEvents = EventsRepository.GetEvents(true); // Include dismissed events

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

            if (notifiedEvents.Count > 0 && this.Visibility == Visibility.Hidden)
            {
                if (!IsMessageBoxOpen("Wagemaster Payroll & HR")) // Use your MessageBox caption here
                {
                    MessageBoxResult result = MessageBox.Show($"There are  {notifiedEvents.Count}  Wagemaster reminders due", "Wagemaster Payroll & HR");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (this.Visibility == Visibility.Hidden)
                        {
                            ShowWindowCommand.Execute(null);
                        }

                    });
                }
            }
        }

        private void ToggleDismissedButton_Click(object sender, RoutedEventArgs e)
        {
            showDismissed = !showDismissed;
            events = new ObservableCollection<Event>(EventsRepository.GetEvents(showDismissed));

            // Update button text
            ToggleDismissedButton.Content = showDismissed ? "Hide Dismissed" : "Show Dismissed";

            LoadEvents();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to save changes?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var viewModel = (MainWindowViewModel)DataContext;
                var selectedEvent = viewModel.SelectedEvent;

                if (selectedEvent != null)
                {
                    EventsRepository.UpdateEvent(selectedEvent);

                    // API update
                    var apiHelper = new ApiHelper();
                    var server = SettingsRepository.GetSettings().Server;
                    var username = SettingsRepository.GetSettings().Username;
                    var password = SettingsRepository.GetSettings().Password;
                    var updatedSuccessfully = await apiHelper.UpdateEventAsync(server, username, password, selectedEvent);

                    /*if (updatedSuccessfully)
                    {
                        Debug.WriteLine("Event updated successfully in API");
                    }
                    else
                    {
                        Debug.WriteLine("Error updating event in API");
                    }*/

                    showDismissed = !showDismissed;
                    events = new ObservableCollection<Event>(EventsRepository.GetEvents(showDismissed));

                    // Update button text
                    ToggleDismissedButton.Content = showDismissed ? "Hide Dismissed" : "Show Dismissed";

                    LoadEvents();
                    
                    // Set the last selected event as the selected event again
                    viewModel.SelectedEvent = selectedEvent;
                }
                MessageBox.Show("Reminder saved successfully.");
            }
        }

        private void SettingsMenuItem_Click(object? sender, RoutedEventArgs e)
        {
            // Open the MainSettings.xaml window
            var settingsWindow = new MainSettings();
            // Subscribe to the event
            settingsWindow.CacheTimeChanged += Settings_CacheTimeChanged;
            
            settingsWindow.ShowDialog();

            // Unsubscribe when the settings window is closed to avoid memory leaks
            settingsWindow.CacheTimeChanged -= Settings_CacheTimeChanged;
        }

        private void Settings_CacheTimeChanged(int newCacheTime)
        {
            //Debug.WriteLine($"Settings_CacheTimeChanged");
            // Here, change your timer interval according to the new cache time.
            apiFetchTimer.Stop();
            
            apiFetchTimer.Interval = SettingsRepository.GetSettings().CacheTime * 1000 * 60;

            apiFetchTimer.Start();
        }

        public ICommand ShowWindowCommand { get; }

        private void ShowWindow(object? _ = null)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }
        private void MinimiseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Hide();
            //this.WindowState = WindowState.Minimized;
        }
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (minimizeToTray)
            {
                e.Cancel = true; // Cancel the closing event
                Hide(); // Hide the window instead of closing
            }
        }


    }
}
