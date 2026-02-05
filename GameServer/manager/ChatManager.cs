using System;
using System.Threading.Tasks;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;
using StackExchange.Redis;
using Google.Protobuf;

namespace MapleWorldAssignment.GameServer.Manager
{
    public class ChatManager
    {
        private ConnectionMultiplexer _redis;
        private ISubscriber _subscriber;
        private const string ChannelName = "GlobalChat";

        public event Action<ChatMessage> OnMessageFromRedis;

        public ChatManager()
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
                        ChatMessage chatMsg = PacketHandler.Deserialize(body);
                        OnMessageFromRedis?.Invoke(chatMsg);
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

        public void PublishMessage(ChatMessage message)
        {
            if (_subscriber != null && _subscriber.IsConnected())
            {
                byte[] data = message.ToByteArray(); // Just the body is enough if we trust our PacketHandler deserializer on the other side?
                // Wait, ReceiveBroadcast logic in ServerSocket assumes full packet? 
                // No, ServerSocket.Broadcast calls PacketHandler.Serialize(message) which adds header.
                // So here we should pass ONLY the protobuf body (or whatever we decide).
                // Let's pass the raw protobuf body.
                // The subscriber side (above) deserializes it using PacketHandler.Deserialize (which expects just body usually, wait).
                // PacketHandler.Deserialize calls `ChatMessage.Parser.ParseFrom(body)`. This expects only the protobuf data.
                // So publishing `message.ToByteArray()` is correct.
                
                _subscriber.Publish(ChannelName, data);
            }
        }
    }
}
