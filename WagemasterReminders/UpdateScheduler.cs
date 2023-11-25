using System.Timers;

namespace YourProjectName.Models
{
    public class UpdateScheduler
    {
        private readonly System.Timers.Timer updateCheckTimer;
        private readonly UpdateChecker updateChecker;
        private bool isFirstExecution = true;

        public UpdateScheduler(string currentVersion)
        {
            updateChecker = new UpdateChecker(currentVersion);

            // Set initial delay to 1 minute
            updateCheckTimer = new System.Timers.Timer(60000);
            updateCheckTimer.Elapsed += OnUpdateCheckTimerElapsed;
            updateCheckTimer.AutoReset = false; // Initially, don't auto-reset
        }

        private void OnUpdateCheckTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            updateChecker.CheckForUpdatesAsync().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    // Log the exception or handle it
                    Console.WriteLine("Error checking for updates: " + task.Exception);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            if (isFirstExecution)
            {
                // After first execution, set interval to 10 hours and enable auto-reset
                updateCheckTimer.Interval = 36000000; // 10 hours
                updateCheckTimer.AutoReset = true;
                isFirstExecution = false;
                updateCheckTimer.Start(); // Restart the timer with the new interval
            }
        }

        public void Start()
        {
            updateCheckTimer.Start();
        }

        public void Stop()
        {
            updateCheckTimer.Stop();
        }
    }
}
