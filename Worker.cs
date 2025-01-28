using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            // Load script configurations
            var scriptConfigs = LoadScriptConfigurations();

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var scriptConfig in scriptConfigs)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Executing script: {scriptName} at: {time}", scriptConfig.Name, DateTimeOffset.Now);
                    }

                    // Execute PowerShell script
                    ExecutePowerShellScript(scriptConfig.Name);

                    // Wait for the specified frequency before executing the next script
                    await Task.Delay(scriptConfig.Frequency, stoppingToken);
                }
            }
        }

        private void ExecutePowerShellScript(string scriptName)
        {
            // Get scripts directory from configuration
            string scriptsDirectory = Path.Combine(AppContext.BaseDirectory, 
                _configuration.GetValue<string>("ScriptSettings:ScriptsDirectory") ?? "scripts");

            string scriptFile = Path.Combine(scriptsDirectory, scriptName);

            if (!File.Exists(scriptFile))
            {
                throw new FileNotFoundException($"The script '{scriptFile}' does not exist.");
            }

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

        private List<ScriptConfig> LoadScriptConfigurations()
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
        public required string Name { get; set; }
        public int Frequency { get; set; } // Frequency in milliseconds
    }
}
