using System;
using System.Threading.Tasks;
using MapleWorldAssignment.GameServer.Manager;
using MapleWorldAssignment.GameServer.Network;
using MapleWorldAssignment.Common.Utility;

namespace MapleWorldAssignment.GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Info("Starting GameServer...");

            ChatManager chatManager = new ChatManager();
            ServerSocket serverSocket = new ServerSocket();

            // When server receives packet -> Publish to Redis
            serverSocket.OnMessageReceived += (msg) => {
                // Console.WriteLine($"[Packet] Received from {msg.UserId}: {msg.Content}");
                chatManager.PublishMessage(msg);
            };

            // When Redis receives message -> Broadcast to all clients
            chatManager.OnMessageFromRedis += (msg) => {
                // Console.WriteLine($"[Redis] Broadcast from {msg.UserId}: {msg.Content}");
                serverSocket.Broadcast(msg);
            };

            // Start Server
            int port = ConfigManager.Instance.ServerPort;
            await serverSocket.StartAsync(port);
        }
    }
}
