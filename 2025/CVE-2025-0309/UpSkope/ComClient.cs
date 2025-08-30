using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace UpSkope
{
    public class ComClient : IDisposable
    {
        private readonly string serverIp = "127.0.0.1";
        private int serverPort = 8888;

        private readonly TcpClient client;
        private NetworkStream stream;

        /*
        Sample initialization request packet:
        00 00 00 1F                             (length of rest of the packet (after this field))
        4E 65 74 53 6B 30 70 65                 (NetSk0pe signature)
        00 00 00 01                             (command ID)
        00 11                                   (length of rest of the packet (after this field))
        00 00 00 0D                             (length of the client name)
        6E 73 43 6C 69 65 6E 74 55 49 5F 73 31  (client name: nsClientUI_s1)

        Sample initialization response packet:
        00 00 00 0E                             (length of rest of the packet (after this field))
        4E 65 74 53 6B 30 70 65                 (NetSk0pe signature)
        01 01 00 00 00 00                       (unknown)

        Sample command request packet:
        00 00 00 0A                             (length of rest of the packet (after this field))
        7B 22 31 30 39 22 3A 22 22 7D           (command: {"109":""})

        Sample command response packet:
        00 00 00 12                             (length of rest of the packet (after this field))
        7B 0A 20 20 20 20 22 31 31 30           (response: {"110":""})
        22 3A 20 22 22 0A 7D 0A
        */

        public ComClient()
        {
            client = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            if (!client.Connected)
            {
                await client.ConnectAsync(serverIp, serverPort);
                stream = client.GetStream();
            }
        }

        private void LogHexDump(string prefix, byte[] data)
        {
            const int bytesPerLine = 16;
            var hexDump = new StringBuilder();

            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                hexDump.Append($"{i:X4}: ");
                var lineBytes = data.Skip(i).Take(bytesPerLine);
                foreach (var b in lineBytes)
                {
                    hexDump.Append($"{b:X2} ");
                }

                if (data.Length - i < bytesPerLine)
                {
                    int padding = (bytesPerLine - (data.Length - i)) * 3;
                    hexDump.Append(new string(' ', padding));
                }

                hexDump.Append(" | ");
                foreach (var b in lineBytes)
                {
                    hexDump.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }

                hexDump.AppendLine();
            }

            Console.WriteLine($"{prefix}:\n{hexDump}");
        }

        private async Task<int> ReadLengthAsync()
        {
            byte[] lengthBuffer = new byte[4];
            await stream.ReadAsync(lengthBuffer, 0, 4);

            // Convert from big-endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            return BitConverter.ToInt32(lengthBuffer, 0);
        }

        public async Task<bool> InitializeConnectionAsync(string clientName = "nsClientUI_s4", string signature = "NetSk0pe")
        {
            byte[] initPacket = CreateInitializationPacket(clientName, signature);
            LogHexDump("Sending initialization packet", initPacket);
            await stream.WriteAsync(initPacket, 0, initPacket.Length);

            // Read response
            int responseLength = await ReadLengthAsync();
            Console.WriteLine($"Response length: {responseLength}");

            byte[] responseBuffer = new byte[responseLength];
            await stream.ReadAsync(responseBuffer, 0, responseLength);

            byte[] fullResponse = BitConverter.GetBytes(responseLength)
                .Reverse()
                .Concat(responseBuffer)
                .ToArray();
            LogHexDump("Received initialization response", fullResponse);

            return true;
        }

        private byte[] CreateInitializationPacket(string clientName = "nsClientUI_s4", string signature = "NetSk0pe")
        {
            using (var stream = new MemoryStream())
            using (var byteWriter = new BinaryWriter(stream))
            {
                byteWriter.WriteInt32BE(
                    signature.Length + 
                    sizeof(Int32) + 
                    sizeof(Int16) + 
                    sizeof(Int32) + 
                    clientName.Length
                    );

                byteWriter.Write(Encoding.UTF8.GetBytes(signature));
                byteWriter.WriteInt32LE(0x01);
                byteWriter.WriteInt16BE((Int16)(sizeof(Int32) + clientName.Length));
                byteWriter.WriteInt32BE(clientName.Length);
                byteWriter.Write(Encoding.UTF8.GetBytes(clientName));
                return stream.ToArray();
            }
        }

        public async Task SendCommandAsync(string command)
        {
            byte[] commandPacket = CreateCommandPacket(command);
            LogHexDump("Sending command packet", commandPacket);
            await stream.WriteAsync(commandPacket, 0, commandPacket.Length);
        }

        private byte[] CreateCommandPacket(string command)
        {
            using (var stream = new MemoryStream())
            using (var byteWriter = new BinaryWriter(stream))
            {
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                byteWriter.WriteInt32BE(commandBytes.Length);
                byteWriter.Write(commandBytes);
                return stream.ToArray();
            }
        }

        public async Task<string> ReceiveResponseAsync()
        {
            int responseLength = await ReadLengthAsync();
            Console.WriteLine($"Response length: {responseLength}");

            byte[] responseBuffer = new byte[responseLength];
            await stream.ReadAsync(responseBuffer, 0, responseLength);

            byte[] fullResponse = BitConverter.GetBytes(responseLength)
                .Reverse()
                .Concat(responseBuffer)
                .ToArray();
            LogHexDump("Received response packet", fullResponse);

            return Encoding.UTF8.GetString(responseBuffer);
        }

        public void Close()
        {
            stream?.Dispose();
            client?.Close();
            client?.Dispose();
        }

        public void Dispose()
        {
            Close();
        }

        public void SetPort(int port)
        {
            serverPort = port;
        }
    }
}