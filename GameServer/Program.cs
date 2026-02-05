using System;
using System.Threading.Tasks;
using MapleWorldAssignment.GameServer.Manager;
using MapleWorldAssignment.GameServer.Network;
using MapleWorldAssignment.Common.Utility;
using MapleWorldAssignment.Common.Protocol;

namespace MapleWorldAssignment.GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Info("Starting GameServer...");

            var chatManager = ChatManager.Instance;
            var serverSocket = ServerSocket.Instance;

            // Register Packet Handler
            PacketDispatcher.RegisterHandler<ChatMessage>(PacketType.Chat, (msg) => {
                 // For now, we assume msg doesn't have sender ID populated from packet if it was just payload
                 // But we want to broadcast it via Redis.
                 chatManager.PublishMessage(msg);
            });

             // When server receives packet -> Publish to Redis
             // Now handled via Dispatcher above. But we need to connect ServerSocket to Dispatcher?
             // ServerSocket calls Dispatcher.Dispatch(packet).
             // Dispatcher calls the handler registered above.
             // Handler calls chatManager.PublishMessage.
             // Cycle complete.

            // When Redis receives message -> Broadcast to all clients
            chatManager.OnMessageFromRedis += (packet) => {
                // Console.WriteLine($"[Redis] Broadcast from {msg.UserId}: {msg.Content}");
                serverSocket.Broadcast(packet);
            };

            // Start Server
            int port = ConfigManager.Instance.ServerPort;
            await serverSocket.StartAsync(port);
        }
    }
}
