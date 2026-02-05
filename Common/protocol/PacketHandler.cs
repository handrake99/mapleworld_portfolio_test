using System;
using System.IO;
using Google.Protobuf;

namespace MapleWorldAssignment.Common.Protocol
{
    public static class PacketHandler
    {
        // 4 bytes header for size
        public const int HeaderSize = 4;

        public static byte[] Serialize(IMessage message)
        {
            int size = message.CalculateSize();
            byte[] buffer = new byte[HeaderSize + size];

            // Write size to header (Big Endian or Little Endian? .NET uses Little Endian by default on Intel, 
            // but network byte order is usually Big Endian. 
            // However, simplicity is key here, let's use BitConverter default (Little Endian on x64) and consisteny.)
            // Let's stick to Little Endian for simplicity in this specific task unless Big Endian is required.
            // Actually, `BinaryWriter` uses Little Endian.
            
            byte[] sizeBytes = BitConverter.GetBytes(size);
            if (!BitConverter.IsLittleEndian)
            {
                // Ensure Little Endian for consistency if running on BE machine
                Array.Reverse(sizeBytes);
            }
            Array.Copy(sizeBytes, 0, buffer, 0, HeaderSize);

            // Write body
            message.WriteTo(new CodedOutputStream(buffer.AsSpan(HeaderSize).ToArray())); // CodedOutputStream writes to stream or byte array
            
            // Optimization: Write directly to buffer
            using (var output = new CodedOutputStream(buffer))
            {
                // CodedOutputStream doesn't support seeking/offset easily with constructor taking byte[] directly for full buffer?
                // Actually CodedOutputStream has no constructor for byte[] with offset.
                // We can use message.WriteTo(Span) if available in newer protobuf, but let's be safe.
                // Let's use ToByteArray() then copy.
                byte[] body = message.ToByteArray();
                Array.Copy(body, 0, buffer, HeaderSize, body.Length);
            }
            
            // Re-do simpler:
            return SerializeSimple(message);
        }

        private static byte[] SerializeSimple(IMessage message)
        {
            byte[] body = message.ToByteArray();
            int size = body.Length;
            byte[] header = BitConverter.GetBytes(size);
            
            byte[] packet = new byte[HeaderSize + size];
            Array.Copy(header, 0, packet, 0, HeaderSize);
            Array.Copy(body, 0, packet, HeaderSize, size);
            
            return packet;
        }

        public static GamePacket Deserialize(byte[] body)
        {
            return GamePacket.Parser.ParseFrom(body);
        }

        public static int ParseHeader(byte[] header)
        {
            if (header.Length < HeaderSize) return 0;
            return BitConverter.ToInt32(header, 0);
        }
    }
}
