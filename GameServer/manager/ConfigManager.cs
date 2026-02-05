using System.IO;
using System.Text.Json;
using Common.Core;

namespace MapleWorldAssignment.GameServer.Manager
{
    public enum EnviromentType
    {
        Dev,
        Staging,
        Production
    }
    public class ConfigManager : Singleton<ConfigManager>
    {
        public int ServerPort { get; private set; }
        private int DefaultServerPort => 3000;
        public string RedisConnectionString { get; private set; }
        private string DefaultRedisConnectionString => "localhost:6379,abortConnect=false";

        private class ConfigData
        {
            public int ServerPort { get; set; }
            public string RedisConnectionString { get; set; }
        }

        private ConfigManager()
        {
            // Initialize with defaults or load from config file/environment
            LoadConfig();
        }

        private void LoadConfig()
        {
            string env = Environment.GetEnvironmentVariable("Env");
            string configFileName;

            if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
            {
                configFileName = "ConfigProduction.json";
            }
            else if (string.Equals(env, "Staging", StringComparison.OrdinalIgnoreCase))
            {
                configFileName = "ConfigStaging.json";
            }
            else
            {
                // Default to Dev if "Dev" or null/unknown
                configFileName = "ConfigDev.json";
            }

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", configFileName);

            if (File.Exists(configPath))
            {
                try 
                {
                    string jsonString = File.ReadAllText(configPath);
                    var configData = JsonSerializer.Deserialize<ConfigData>(jsonString);
                    
                    if (configData != null)
                    {
                        ServerPort = configData.ServerPort;
                        RedisConnectionString = configData.RedisConnectionString;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load config file {configPath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Config file not found: {configPath}");
            }

            // Fallback to defaults if loading fails
            ServerPort = DefaultServerPort;
            RedisConnectionString = DefaultRedisConnectionString;
        }
    }
}
