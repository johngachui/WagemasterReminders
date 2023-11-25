using System.Timers;
namespace YourProjectName.Models
{
    public class UpdateScheduler
    {
        private readonly System.Timers.Timer updateCheckTimer;
        private readonly UpdateChecker updateChecker;

        public UpdateScheduler(string currentVersion, double interval)
        {
            updateChecker = new UpdateChecker(currentVersion);

            updateCheckTimer = new System.Timers.Timer(interval);
            updateCheckTimer.Elapsed += OnUpdateCheckTimerElapsed;
            updateCheckTimer.AutoReset = true;
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
