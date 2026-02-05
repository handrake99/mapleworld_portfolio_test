using System;
using System.Threading.Tasks;
using Common.Core;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;
using StackExchange.Redis;
using Google.Protobuf;

namespace MapleWorldAssignment.GameServer.Manager
{
    public class ChatManager : Singleton<ChatManager>
    {
        private ConnectionMultiplexer _redis;
        private ISubscriber _subscriber;
        private const string ChannelName = "GlobalChat";

        private ChatManager()
        {
            // Use ConfigManager for connection string
            string redisConnectionString = ConfigManager.Instance.RedisConnectionString;
            try
            {
                _redis = ConnectionMultiplexer.Connect(redisConnectionString);
                _subscriber = _redis.GetSubscriber();
                
                _subscriber.Subscribe(ChannelName, (channel, message) =>
                {
                    try 
                    {
                        byte[] body = (byte[])message;
                        // Redis message is now expected to be 'GamePacket' protobuf data?
                        // Or just ChatMessage data?
                        // If we publish 'GamePacket', we can extend to SystemMessages via Redis too.
                        // Let's assume we publish GamePacket bytes.
                        GamePacket packet = GamePacket.Parser.ParseFrom(body);
                        
                        // We need to notify ServerSocket to broadcast this packet.
                        // PacketDispatcher logic is mostly for INCOMING client packets.
                        // But here we are just relaying.
                        
                        // Wait, ChatManager event is `Action<ChatMessage> OnMessageFromRedis`.
                        // ServerSocket expects `Broadcast(GamePacket)`.
                        
                        // If we receive a GamePacket (which contains Type + Payload),
                        // and we want to invoke OnMessageFromRedis which is Action<GamePacket> now?
                        
                        OnMessageFromRedis?.Invoke(packet);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[ChatManager] Error parsing Redis message: {ex.Message}");
                    }
                });
                
                Logger.Info("[ChatManager] Connected to Redis and subscribed to GlobalChat.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ChatManager] Failed to connect to Redis: {ex.Message}");
            }
        }

        public event Action<GamePacket> OnMessageFromRedis;

        public void PublishMessage(ChatMessage message)
        {
            if (_subscriber != null && _subscriber.IsConnected())
            {
                // Wrap in GamePacket
                GamePacket packet = new GamePacket
                {
                    Type = PacketType.Chat,
                    Payload = message.ToByteString()
                };
                
                byte[] data = packet.ToByteArray();
                
                _subscriber.Publish(ChannelName, data);
            }
        }
    }
}
