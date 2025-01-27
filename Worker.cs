using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace WorkerServiceMCT
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                // Execute PowerShell scripts
                ExecutePowerShellScripts();

                await Task.Delay(1000, stoppingToken);
            }
        }

        private void ExecutePowerShellScripts()
        {
            // Get scripts directory from configuration
            string scriptsDirectory = Path.Combine(AppContext.BaseDirectory, 
                _configuration.GetValue<string>("ScriptSettings:ScriptsDirectory") ?? "scripts");

            if (!Directory.Exists(scriptsDirectory))
            {
                throw new DirectoryNotFoundException($"The directory '{scriptsDirectory}' does not exist.");
            }

            string[] scriptFiles = Directory.GetFiles(scriptsDirectory, "*.ps1");

            foreach (var scriptFile in scriptFiles)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Append output to output.txt
                    File.AppendAllText("output.txt", output);
                }
            }
        }
    }
}
