using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace UpSkope
{
    class TestPipeClient
    {
        static async Task Main(string[] args)
        {
            const string PIPE_NAME = "\\\\.\\pipe\\UpSkopeIPCv2";

            Console.WriteLine($"Connecting to pipe '{PIPE_NAME}'...");

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut, PipeOptions.None))
                {
                    // Connect with timeout
                    await pipeClient.ConnectAsync(10000);
                    Console.WriteLine("Connected to pipe");

                    // Read handshake
                    byte[] handshakeBuffer = new byte[1024];
                    int handshakeBytesRead = await pipeClient.ReadAsync(handshakeBuffer, 0, handshakeBuffer.Length);

                    if (handshakeBytesRead > 0)
                    {
                        string handshakeMsg = Encoding.UTF8.GetString(handshakeBuffer, 0, handshakeBytesRead);
                        Console.WriteLine($"Received handshake: {handshakeMsg}");
                    }
                    else
                    {
                        Console.WriteLine("No handshake received");
                        return;
                    }

                    // Send command
                    string command = "Test command from client";
                    byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                    await pipeClient.WriteAsync(commandBytes, 0, commandBytes.Length);
                    await pipeClient.FlushAsync();
                    Console.WriteLine($"Sent command: {command}");

                    // Read response
                    byte[] responseBuffer = new byte[4096];
                    int bytesRead = await pipeClient.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                        Console.WriteLine($"Received response: {response}");
                    }
                    else
                    {
                        Console.WriteLine("No response received");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("Test completed");
            Console.ReadLine();
        }
    }
} 