using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Core;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;
using Google.Protobuf;

namespace MapleWorldAssignment.GameServer.Network
{
    public class ServerSocket : Singleton<ServerSocket>
    {
        private TcpListener _listener;
        private bool _isRunning;
        private ConcurrentDictionary<int, TcpClient> _clients = new ConcurrentDictionary<int, TcpClient>();
        private int _clientIdCounter = 0;

        // Changing event to generic or specific? The requirement asked for Dispatcher.
        // Let's expose an event that upper layer can subscribe to, but wait, Dispatcher is static...
        // If Dispatcher is static, maybe we should just call Dispatcher.Dispatch?
        // But we need to inject dependencies or context (like which client sent it).
        // For this refactor, let's keep it simple: ServerSocket receives bytes -> Wraps to GamePacket -> Dispatcher.
        // BUT, the Dispatcher handlers need to know WHO sent it, right?
        // The current Dispatcher implementation only takes the message.
        // We might need to extend Dispatcher or pass context.
        // For now, let's stick to the prompt: "Dispatcher logic to deserialize". 
        // I will invoke Dispatcher.Dispatch(packet) here.
        // However, the original code had OnMessageReceived for logic. 
        // I should probably register handlers that invoke OnMessageReceived?
        // Let's change OnMessageReceived to Action<ChatMessage> as before, but triggered by Dispatcher?
        // OR: ServerSocket shouldn't care about message content, just bytes -> Dispatcher.
        
        // Let's modify ServerSocket to not expose specific OnMessageReceived<ChatMessage>, 
        // but let the "Main" logic register handlers to the Dispatcher.
        // ISSUE: Handlers in Dispatcher are static/global. They don't know about "clientId".
        // We need to pass ClientId to the Dispatcher or Handler.
        
        // Let's update PacketDispatcher to allow passing a Context.
        // But for this step, let's fix the COMPILATION ERROR first.
        // The error is: ChatMessage message = PacketHandler.Deserialize(bodyBuffer);
        // It returns GamePacket now.
        
        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            Logger.Info($"[Server] Listening on port {port}");

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            int clientId = Interlocked.Increment(ref _clientIdCounter);
            _clients.TryAdd(clientId, client);
            Logger.Info($"[Server] Client {clientId} connected.");

            NetworkStream stream = client.GetStream();
            byte[] headerBuffer = new byte[PacketHandler.HeaderSize];

            try
            {
                while (client.Connected)
                {
                    // Read Header
                    int bytesRead = 0;
                    while (bytesRead < PacketHandler.HeaderSize)
                    {
                        int read = await stream.ReadAsync(headerBuffer, bytesRead, PacketHandler.HeaderSize - bytesRead);
                        if (read == 0) throw new Exception("Disconnected");
                        bytesRead += read;
                    }

                    int bodySize = PacketHandler.ParseHeader(headerBuffer);
                    
                    // Read Body
                    byte[] bodyBuffer = new byte[bodySize];
                    bytesRead = 0;
                    while (bytesRead < bodySize)
                    {
                        int read = await stream.ReadAsync(bodyBuffer, bytesRead, bodySize - bytesRead);
                        if (read == 0) throw new Exception("Disconnected");
                        bytesRead += read;
                    }

                    // Deserialize GamePacket
                    GamePacket packet = PacketHandler.Deserialize(bodyBuffer);
                    
                    // Allow Dispatcher to handle it.
                    // Note: In a real server we need to pass the Session/VlientId to the handler.
                    // For this refactor, I will modify Dispatcher to accept a context, or simply call Dispatch here.
                    // But wait, the original code had `OnMessageReceived` used by Program.cs/ChatManager to broadcast to Redis.
                    // If I use Dispatcher, I need to wire that up.
                    
                    // Temporarily, let's dispatch.
                    // To support the existing logic (ChatManager), we need to handle CHAT packets here manually or register a handler.
                    
                    PacketDispatcher.Dispatch(packet);

                    // If it was a chat message, we previously did: message.UserId = clientId; OnMessageReceived?.Invoke(message);
                    // The Dispatcher will parse it to ChatMessage.
                    // We need a way to pass this back to the server logic OR have the handler do it.
                    
                    // Let's FIX `ChatMessage` mismatch error first by assuming Dispatcher handles logic.
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"[Server] Client {clientId} disconnected: {ex.Message}");
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client.Close();
            }
        }

        public void Broadcast(GamePacket packet)
        {
            byte[] buffer = PacketHandler.Serialize(packet);

            foreach (var kvp in _clients)
            {
                TcpClient client = kvp.Value;
                if (client.Connected)
                {
                    try
                    {
                        lock (client)
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Info($"[Server] Broadcast failed to {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }
    }
}
