using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace UpSkope
{
    public static class EntryPointResolver
    {
        private static IntPtr GetBaseAddress32(IntPtr hProc)
        {
            int status = NtQueryInformationProcess(
                hProc,
                0, // ProcessBasicInformation
                out PROCESS_BASIC_INFORMATION pbi,
                Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(),
                out _
            );

            if (status != 0)
                throw new Win32Exception($"NtQueryInformationProcess failed (status {status})");

            // ImageBaseAddress is at offset 0x08 in 32-bit
            if (!ReadProcessMemory(hProc, pbi.PebBaseAddress + 0x08, out IntPtr baseAddress, 4, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ReadProcessMemory failed");

            return baseAddress;
        }

        private static uint GetEntryPointRva(string exePath)
        {
            using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // Read DOS header
                fs.Seek(0x3C, SeekOrigin.Begin);
                int peOffset = reader.ReadInt32();

                // Go to PE header
                fs.Seek(peOffset, SeekOrigin.Begin);
                uint peSignature = reader.ReadUInt32();
                
                if (peSignature != 0x4550)
                    throw new InvalidDataException("Invalid PE signature");

                // Skip COFF header
                fs.Seek(20, SeekOrigin.Current);

                // Validate optional header magic
                ushort magic = reader.ReadUInt16();
                if (magic != 0x10B)
                    throw new InvalidDataException("Not a PE32 executable");

                // Skip to AddressOfEntryPoint
                fs.Seek(14, SeekOrigin.Current);
                uint addressOfEntryPoint = reader.ReadUInt32();

                return addressOfEntryPoint;
            }
        }

        public static IntPtr GetEntryPoint(IntPtr hProc, string exePath)
        {
            IntPtr baseAddr = GetBaseAddress32(hProc);
            uint entryPointRva = GetEntryPointRva(exePath);
            return IntPtr.Add(baseAddr, (int)entryPointRva);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public IntPtr[] Reserved2;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            out PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            out IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead
        );
    }
}