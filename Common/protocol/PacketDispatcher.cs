using Google.Protobuf;

namespace MapleWorldAssignment.Common.Protocol
{
    public static class PacketDispatcher
    {
        // Define a delegate for handling parsed messages
        // We use IMessage interface to keep it generic, but handlers will cast it.
        public delegate void MessageHandler(IMessage message);

        private static readonly Dictionary<PacketType, MessageHandler> _handlers = new();

        /// <summary>
        /// Registers a handler for a specific message type.
        /// </summary>
        public static void RegisterHandler<T>(PacketType type, Action<T> handler) where T : IMessage<T>, new()
        {
            if (_handlers.ContainsKey(type))
            {
                _handlers.Remove(type);
            }

            // Wrap the specific handler action into a generic MessageHandler
            _handlers.Add(type, (message) => handler((T)message));
        }

        /// <summary>
        /// Dispatches the GamePacket to the appropriate handler.
        /// </summary>
        public static void Dispatch(GamePacket packet)
        {
            if (!_handlers.TryGetValue(packet.Type, out var handler))
            {
                Console.WriteLine($"[PacketDispatcher] No handler registered for PacketType: {packet.Type}");
                return;
            }

            try
            {
                IMessage message = ParsePayload(packet.Type, packet.Payload);
                if (message != null)
                {
                    handler(message);
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"[PacketDispatcher] Failed to parse payload for {packet.Type}: {ex.Message}");
            }
        }

        /// <summary>
        /// Efficiently parses the payload based on PacketType.
        /// </summary>
        private static IMessage ParsePayload(PacketType type, ByteString payload)
        {
            // SWITCH-based dispatch is the fastest and allocates the least memory 
            // compared to Reflection or Dictionary<Type, Parser> for a small enum.
            switch (type)
            {
                case PacketType.Chat:
                    return ChatMessage.Parser.ParseFrom(payload);
                case PacketType.System:
                    return SystemMessage.Parser.ParseFrom(payload);
                default:
                    Console.WriteLine($"[PacketDispatcher] Unknown PacketType: {type}");
                    return null;
            }
        }
    }
}
