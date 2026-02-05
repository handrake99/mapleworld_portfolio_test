using System;

namespace MapleWorldAssignment.GameServer.Manager
{
    public class ConfigManager
    {
        private static Lazy<ConfigManager> _instance = new Lazy<ConfigManager>(() => new ConfigManager());

        public static ConfigManager Instance => _instance.Value;

        public int ServerPort { get; private set; }
        public string RedisConnectionString { get; private set; }

        private ConfigManager()
        {
            // Initialize with defaults or load from config file/environment
            LoadConfig();
        }

        private void LoadConfig()
        {
            // For this assignment, we use defaults or environment variables
            // Port
            string? portEnv = Environment.GetEnvironmentVariable("SERVER_PORT");
            ServerPort = int.TryParse(portEnv, out int port) ? port : 3000;

            // Redis
            string? redisEnv = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            RedisConnectionString = !string.IsNullOrEmpty(redisEnv) 
                ? redisEnv 
                : "localhost:6379,abortConnect=false";
        }
    }
}
