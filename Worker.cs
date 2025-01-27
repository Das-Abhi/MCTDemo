using System.Diagnostics;

namespace WorkerServiceMCT
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
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
            string scriptsDirectory = @"C:\Users\dabhi\source\repos\WorkerServiceMCT\WorkerServiceMCT\scripts";
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
