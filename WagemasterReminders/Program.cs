
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourProjectName.Services;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

//Stop multiple loads

using System.Threading;
using YourProjectName.Models; // Add the namespace where UpdateScheduler is located

bool createdNew;
var mutex = new Mutex(true, "Wagemaster API", out createdNew);

if (!createdNew)
{
    // If the mutex already exists, another instance of the application is running
    MessageBox.Show("Another instance of Wagemaster API is already running.");
    return;
}

// Rest of the application code goes here

var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--console").ToArray());

// Add services to the container.
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IReminderService, ReminderService>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

var app = builder.Build();

// Initialize UpdateScheduler
var updateScheduler = new UpdateScheduler("1.0.0"); //Replace with your current version and desired interval
// Instantiate UpdateChecker for context menu
var updateChecker = new UpdateChecker("1.0.0"); // Replace "1.0.0" with the current version of your application

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Use the registered services to configure the app.
var serviceProvider = app.Services;
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Wagemaster API...");

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Start the API in a separate thread
var apiCts = new CancellationTokenSource();
var apiThread = new Thread(() =>
{
    try
    {
        app.RunAsync(apiCts.Token).Wait();
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation
    }
});
apiThread.Start();

// Start the UpdateScheduler in a separate thread
var schedulerCts = new CancellationTokenSource();
var schedulerThread = new Thread(() =>
{
    try
    {
        updateScheduler.Start(); // Start the UpdateScheduler
        while (!schedulerCts.Token.IsCancellationRequested)
        {
            Thread.Sleep(1000); // Adjust the sleep time as needed
        }
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation
    }
});
schedulerThread.Start();


// Create the notify icon and add a context menu with a quit option
var notifyIcon = new NotifyIcon
{
    Icon = new Icon(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "WagemasterAPI.ico")),
    //Icon = SystemIcons.Application,
    Visible = true,
    Text = "Wagemaster API v 1.0.0"
};
var contextMenuStrip = new ContextMenuStrip();
// Add 'Check for Update' menu item
var checkForUpdateToolStripMenuItem = new ToolStripMenuItem("Check for Update");
checkForUpdateToolStripMenuItem.Click += async (sender, args) =>
{
    // Call the method to check for updates
    await updateChecker.CheckForUpdatesAsync();
};
contextMenuStrip.Items.Add(checkForUpdateToolStripMenuItem);

// Add 'Quit' menu item
var quitToolStripMenuItem = new ToolStripMenuItem("Quit");
quitToolStripMenuItem.Click += (sender, args) =>
{
    notifyIcon.Visible = false;
    apiCts.Cancel();
    schedulerCts.Cancel();
    apiThread.Join();
    schedulerThread.Join();
    Application.Exit();
};
contextMenuStrip.Items.Add(quitToolStripMenuItem);
notifyIcon.ContextMenuStrip = contextMenuStrip;

// Attach a MouseClick event handler to the notify icon
notifyIcon.MouseClick += (sender, args) =>
{
    if (args.Button == MouseButtons.Right)
    {
        // Show the context menu at the current mouse position
        contextMenuStrip.Show(Cursor.Position);
    }
};

// Attach a FormClosing event handler to dispose of the notify icon
Application.ApplicationExit += (sender, args) =>
{
    updateScheduler.Stop(); // Stop the UpdateScheduler
    notifyIcon.Visible = false;
    notifyIcon.Dispose();
    schedulerCts.Cancel();
    schedulerThread.Join();
};

Application.Run();
// Wait for the API thread to finish
apiThread.Join();
schedulerThread.Join();
