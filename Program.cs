using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkerServiceMCT;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHangfire(config => config.UseMemoryStorage()); // Registers Hangfire
        services.AddHangfireServer(); // Starts Hangfire Server

        services.AddSingleton<Worker>(); // Explicitly register Worker
        services.AddHostedService<Worker>(); // Register Worker as a BackgroundService
    });

var app = builder.Build();

// Start Hangfire Server
using (var scope = app.Services.CreateScope())
{
    var worker = scope.ServiceProvider.GetRequiredService<Worker>(); // Get Worker from DI
    var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>(); // Use DI-based API

    var scriptConfigs = worker.LoadScriptConfigurations();
    foreach (var scriptConfig in scriptConfigs)
    {
        string cronExpression = worker.ConvertMillisecondsToCron(scriptConfig.Frequency);

        jobManager.AddOrUpdate(
            scriptConfig.Name,
            () => worker.ExecutePowerShellScript(scriptConfig.Name),
            cronExpression);
    }
}

// Create a Minimal API for Hangfire Dashboard
var dashboardBuilder = WebApplication.CreateBuilder();
dashboardBuilder.Services.AddHangfire(config => config.UseMemoryStorage());
dashboardBuilder.Services.AddHangfireServer();

var dashboardApp = dashboardBuilder.Build();
dashboardApp.UseHangfireDashboard();

await Task.WhenAll(
    dashboardApp.RunAsync(), // Run Hangfire Dashboard
    app.RunAsync()           // Run Worker Service
);
