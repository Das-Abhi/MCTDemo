using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Hangfire;

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
        public string ConvertMillisecondsToCron(int milliseconds)
        {
            int minutes = milliseconds / 60000; // Convert ms to minutes

            if (minutes < 1)
                return "* * * * *"; // Run every minute (minimum allowed interval)

            if (minutes >= 60)
            {
                int hours = minutes / 60;
                return $"0 */{hours} * * *"; // Run every X hours
            }

            return $"*/{minutes} * * * *"; // Run every X minutes
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scriptConfigs = LoadScriptConfigurations();

            foreach (var scriptConfig in scriptConfigs)
            {
                string cronExpression = ConvertMillisecondsToCron(scriptConfig.Frequency);

                if (!string.IsNullOrEmpty(cronExpression))
                {
                    RecurringJob.AddOrUpdate(
                        scriptConfig.Name,
                        () => ExecutePowerShellScript(scriptConfig.Name),
                        cronExpression);
                }
                else
                {
                    _logger.LogError("Invalid frequency for script {scriptName}. Skipping scheduling.", scriptConfig.Name);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // Keep the worker running
            }
        }

        //private void ExecutePowerShellScript(string scriptName)
        //{
        //    // Get scripts directory from configuration
        //    string scriptsDirectory = Path.Combine(AppContext.BaseDirectory, 
        //        _configuration.GetValue<string>("ScriptSettings:ScriptsDirectory") ?? "scripts");

        //    string scriptFile = Path.Combine(scriptsDirectory, scriptName);

        //    if (!File.Exists(scriptFile))
        //    {
        //        throw new FileNotFoundException($"The script '{scriptFile}' does not exist.");
        //    }

        //    ProcessStartInfo startInfo = new ProcessStartInfo
        //    {
        //        FileName = "powershell.exe",
        //        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\"",
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };

        //    using (Process process = new Process { StartInfo = startInfo })
        //    {
        //        process.Start();
        //        string output = process.StandardOutput.ReadToEnd();
        //        process.WaitForExit();

        //        // Append output to output.txt
        //        File.AppendAllText("output.txt", output);
        //    }
        //}
        public void ExecutePowerShellScript(string scriptName)
        {
            string scriptsDirectory = Path.Combine(AppContext.BaseDirectory,
                _configuration.GetValue<string>("ScriptSettings:ScriptsDirectory") ?? "scripts");

            string scriptFile = Path.Combine(scriptsDirectory, scriptName);

            if (!File.Exists(scriptFile))
            {
                _logger.LogError($"Script '{scriptFile}' not found.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogError("Error executing script {scriptName}: {error}", scriptName, error);
                }
                else
                {
                    _logger.LogInformation("Executed script {scriptName}: {output}", scriptName, output);
                }

                // ✅ Use FileStream with FileShare.Write to allow concurrent writing
                string logEntry = $"[{DateTime.Now}] {scriptName}: {output}\n";
                AppendToFileThreadSafe("output.txt", logEntry);
            }
        }

        // Thread-safe method for writing to a file
        private void AppendToFileThreadSafe(string filePath, string content)
        {
            for (int i = 0; i < 5; i++) // Retry up to 5 times in case of file access issues
            {
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(content);
                    }
                    return; // Exit loop if write is successful
                }
                catch (IOException ex)
                {
                    _logger.LogWarning($"File access issue: {ex.Message}. Retrying...");
                    Thread.Sleep(100); // Wait 100ms before retrying
                }
            }
            _logger.LogError($"Failed to write to {filePath} after multiple attempts.");
        }


        public List<ScriptConfig> LoadScriptConfigurations()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string configFilePath = Path.Combine(AppContext.BaseDirectory, "ScriptConfig.yml");
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"The configuration file '{configFilePath}' does not exist.");
            }

            var yamlContent = File.ReadAllText(configFilePath);
            return deserializer.Deserialize<List<ScriptConfig>>(yamlContent);
        }
    }

    public class ScriptConfig
    {
        public required string Name { get; set; } // mandatory field 
        public int Frequency { get; set; } // Frequency in milliseconds
    }
}
