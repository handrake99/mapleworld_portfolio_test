using System;
using System.Threading.Tasks;
using MapleWorldAssignment.DummyClient.Test;
using MapleWorldAssignment.Common.Utility;
using MapleWorldAssignment.Common.Protocol;

namespace MapleWorldAssignment.DummyClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Info("Starting DummyClient...");

            var socket = new ClientSocket();
            // Register Handler
            PacketDispatcher.RegisterHandler<ChatMessage>(PacketType.Chat, (msg) => {
                 Logger.Info($"[Client Recv] User {msg.Sender}: {msg.Message}");
            });

            try 
            {
                await socket.ConnectAsync("127.0.0.1", 3000);

                LoadTester tester = new LoadTester(socket);
                
                // Run tester in background but await a bit or let it run
                _ = tester.StartTestAsync(2000); // Send every 2 seconds

                // Keep alive
                while (true)
                {
                   await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error: {ex.Message}");
            }
        }
    }
}
