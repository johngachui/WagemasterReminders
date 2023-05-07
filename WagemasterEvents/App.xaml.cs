using System.Windows;

namespace WagemasterEvents
{
    public partial class App : Application
    {
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Closed += MainWindow_Closed;
            //MainWindow.Hide(); // Hide the main window initially
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            
        }
    }
}
