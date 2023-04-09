using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YourProjectName;
using YourProjectName.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ServiceProcess;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

builder.Host.UseWindowsService();

// Use asynchronous methods
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = false;
});

var app = builder.Build();

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
logger.LogInformation("Starting WagemasterReminders API...");

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
