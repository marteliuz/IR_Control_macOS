using System;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace IR_SecondaryScreen
{
    public class TelnetClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public TelnetClient(string host, int port)
        {
            _client = new TcpClient(host, port);
            _stream = _client.GetStream();
        }

        public void SendCommand(string command)
        {
            if (_client == null || !_client.Connected) return;

            byte[] data = Encoding.ASCII.GetBytes(command);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }

        public void Close()
        {
            _stream?.Close();
            _client?.Close();
        }

        public void Dispose()
        {
            Close();
        }
    }
}