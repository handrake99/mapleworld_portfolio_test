using System;
using System.Threading.Tasks;
using MapleWorldAssignment.Common.Protocol;
using MapleWorldAssignment.Common.Utility;

namespace MapleWorldAssignment.DummyClient.Test
{
    public class LoadTester
    {
        private ClientSocket _socket;
        private Random _random = new Random();
        private bool _isRunning;

        public LoadTester(ClientSocket socket)
        {
            _socket = socket;
        }

        public async Task StartTestAsync(int intervalMs = 1000)
        {
            _isRunning = true;
            Logger.Info("[LoadTester] Started sending messages...");

            while (_isRunning)
            {
                var msg = new ChatMessage
                {
                    UserId = 0, // Server will assign
                    Content = $"Hello from DummyClient at {DateTime.Now.Ticks}"
                };

                await _socket.SendAsync(msg);
                await Task.Delay(intervalMs);
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
