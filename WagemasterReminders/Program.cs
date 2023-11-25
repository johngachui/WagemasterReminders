
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
var updateScheduler = new UpdateScheduler("1.0.0", 120000); //hours x 60 x 60000 Replace with your current version and desired interval


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
var cts = new CancellationTokenSource();
var apiThread = new Thread(() =>
{
    try
    {
        updateScheduler.Start(); // Start the UpdateScheduler
        app.RunAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Ignore the exception
    }
});
apiThread.Start();

// Create the notify icon and add a context menu with a quit option
var notifyIcon = new NotifyIcon
{
    Icon = new Icon(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "WagemasterAPI.ico")),
    //Icon = SystemIcons.Application,
    Visible = true,
    Text = "Wagemaster API v 1.0.0"
};
var contextMenuStrip = new ContextMenuStrip();
var quitToolStripMenuItem = new ToolStripMenuItem("Quit");
quitToolStripMenuItem.Click += (sender, args) =>
{
    notifyIcon.Visible = false;
    cts.Cancel();
    apiThread.Join();
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
};

Application.Run();
// Wait for the API thread to finish
apiThread.Join();
