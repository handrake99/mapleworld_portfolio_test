using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;
using Google.Protobuf;

namespace MapleWorldAssignment.DummyClient.Test
{
    public class ClientSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        
        // Removed specific event to rely on Dispatcher or expose GamePacket?
        // Let's expose GamePacket so Program can decide? 
        // Or better, let's follow the pattern: Dispatcher handles it.
        // But for the Client to be useful, maybe we just expose the Dispatcher or use it internally?
        // Let's just use Dispatcher.Dispatch() here.
        // And Program.cs will register the handler.
        
        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            _isConnected = true;
            Logger.Info($"[Client] Connected to {ip}:{port}");

            _ = ReceiveLoop();
        }

        public async Task SendAsync(ChatMessage message)
        {
            if (!_isConnected) return;

            // Wrap in GamePacket
            GamePacket packet = new GamePacket
            {
                Type = PacketType.Chat,
                Payload = message.ToByteString()
            };

            byte[] buffer = PacketHandler.Serialize(packet);
            try 
            {
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Client] Send failed: {ex.Message}");
                Close();
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] headerBuffer = new byte[PacketHandler.HeaderSize];
            try
            {
                while (_isConnected)
                {
                    // Read Header
                    int bytesRead = 0;
                    while (bytesRead < PacketHandler.HeaderSize)
                    {
                        int read = await _stream.ReadAsync(headerBuffer, bytesRead, PacketHandler.HeaderSize - bytesRead);
                        if (read == 0) throw new Exception("Disconnected");
                        bytesRead += read;
                    }

                    int bodySize = PacketHandler.ParseHeader(headerBuffer);
                    
                    // Read Body
                    byte[] bodyBuffer = new byte[bodySize];
                    bytesRead = 0;
                    while (bytesRead < bodySize)
                    {
                        int read = await _stream.ReadAsync(bodyBuffer, bytesRead, bodySize - bytesRead);
                        if (read == 0) throw new Exception("Disconnected");
                        bytesRead += read;
                    }

                    // Deserialize
                    GamePacket packet = PacketHandler.Deserialize(bodyBuffer);
                    
                    // Dispatch
                    PacketDispatcher.Dispatch(packet);
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"[Client] Disconnected: {ex.Message}");
                Close();
            }
        }

        public void Close()
        {
            _isConnected = false;
            _client?.Close();
        }
    }
}
