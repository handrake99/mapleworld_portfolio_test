using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;
using Google.Protobuf;

namespace MapleWorldAssignment.GameServer.Network
{
    public class ServerSocket
    {
        private TcpListener _listener;
        private bool _isRunning;
        private ConcurrentDictionary<int, TcpClient> _clients = new ConcurrentDictionary<int, TcpClient>();
        private int _clientIdCounter = 0;

        public event Action<ChatMessage> OnMessageReceived;

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

                    // Deserialize
                    ChatMessage message = PacketHandler.Deserialize(bodyBuffer);
                    // Force userId to match connection (optional security measure, or just trust packet?)
                    // For assignment, let's assume we use the packet's userId or overwrite it with session Id.
                    // "Define a ChatMessage in .proto (int32 userId, string content)"
                    // Let's overwrite userId with server-assigned ID so clients can't spoof easily, 
                    // or just pass it through. Let's overwrite to show "Server Logic".
                    message.UserId = clientId;

                    OnMessageReceived?.Invoke(message);
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

        public void Broadcast(ChatMessage message)
        {
            byte[] packet = PacketHandler.Serialize(message);

            foreach (var kvp in _clients)
            {
                TcpClient client = kvp.Value;
                if (client.Connected)
                {
                    try
                    {
                        // Fire and forget send? Or await?
                        // For simplicity in this loop, we can use synchronous Write or fire async task.
                        // NetworkStream.Write is blocking, WriteAsync is not.
                        // To avoid blocking the Redis subscriber thread (which calls this), we should act carefully.
                        // Using WriteAsync without await (fire and forget) might cause concurrency issues on the stream if multiple threads write.
                        // TcpClient is not thread safe for concurrent writes.
                        // We should ideally use a send queue per client.
                        // For this assignment complexity: Just lock and write, or simple await.
                        
                        // NOTE: In a real high-perf server, we'd use a SendQueue. 
                        // Here: Lock the stream to ensure atomic writes.
                        lock (client)
                        {
                            NetworkStream stream = client.GetStream();
                            stream.Write(packet, 0, packet.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[Server] Broadcast failed to {kvp.Key}: {ex.Message}");
                    }
                }
            }
        }
    }
}
