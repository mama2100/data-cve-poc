using System;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace UpSkope
{
    internal static class DeviceId
    {
        [DllImport("kernel32")]
        static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [StructLayout(LayoutKind.Sequential)]
        private struct CpuidRegisters
        {
            public int Eax;
            public int Ebx;
            public int Ecx;
            public int Edx;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void CpuidDelegate(int eax, IntPtr registers);

        const uint PAGE_EXECUTE_READWRITE = 0x40;

        private static string GetBiosSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                return "Error getting BIOS serial";
            }
            return string.Empty;
        }

        private static string GetMachineGuid()
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false))
                {
                    if (key != null)
                    {
                        var guid = key.GetValue("MachineGuid") as string;
                        if (!string.IsNullOrEmpty(guid) && guid.Length == 36)
                        {
                            return guid;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "Error reading registry";
            }
            return "Error: Invalid GUID format";
        }

        private static void GetCpuid(out uint eax, out uint ebx, out uint ecx, out uint edx)
        {
            CpuidRegisters registers = new CpuidRegisters();
            IntPtr registersPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CpuidRegisters>());

            try
            {
                // __stdcall void cpuid(int eaxIn, CpuidRegisters* registers)
                byte[] code = new byte[]
                {
                    0x55,                         // push ebp
                    0x89, 0xE5,                   // mov ebp, esp
                    0x56,                         // push esi

                    // Set EAX to input value and save pointer in ESI
                    0x8B, 0x45, 0x08,             // mov eax, [ebp+8]
                    0x8B, 0x75, 0x0C,             // mov esi, [ebp+0xC]
                    0x0F, 0xA2,                   // cpuid

                    // Store results using ESI as pointer
                    0x89, 0x06,                   // mov [esi], eax
                    0x89, 0x5E, 0x04,             // mov [esi+4], ebx
                    0x89, 0x4E, 0x08,             // mov [esi+8], ecx
                    0x89, 0x56, 0x0C,             // mov [esi+12], edx

                    0x5E,                         // pop esi
                    0x5D,                         // pop ebp
                    0xC2, 0x08, 0x00              // ret 0x8
                };

                IntPtr codePtr = Marshal.AllocHGlobal(code.Length);
                try
                {
                    Marshal.Copy(code, 0, codePtr, code.Length);
                    VirtualProtect(codePtr, (UIntPtr)code.Length, PAGE_EXECUTE_READWRITE, out _);

                    var del = Marshal.GetDelegateForFunctionPointer<CpuidDelegate>(codePtr);

                    // Clear registers
                    registers.Eax = 0;
                    registers.Ebx = 0;
                    registers.Ecx = 0;
                    registers.Edx = 0;
                    Marshal.StructureToPtr(registers, registersPtr, false);

                    del(1, registersPtr);

                    // Copy the values
                    registers = Marshal.PtrToStructure<CpuidRegisters>(registersPtr);

                    // Set the output parameters
                    eax = (uint)registers.Eax;
                    ebx = (uint)registers.Ebx;
                    ecx = (uint)registers.Ecx;
                    edx = (uint)registers.Edx;
                }
                finally
                {
                    if (codePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(codePtr);
                    }
                }
            }
            finally
            {
                if (registersPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(registersPtr);
                }
            }
        }

        public static string GenerateDeviceUid()
        {
            GetCpuid(out uint eax, out uint ebx, out uint ecx, out uint edx);
            string cpuid = $"{edx:X8}{eax:X8}";
            string biosSerial = GetBiosSerial();
            string machineGuid = GetMachineGuid();

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(cpuid + biosSerial + machineGuid);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert to hex
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                string hexMd5 = sb.ToString();

                // Format as GUID
                return $"{hexMd5.Substring(0, 8)}-{hexMd5.Substring(8, 4)}-{hexMd5.Substring(12, 4)}-{hexMd5.Substring(16, 4)}-{hexMd5.Substring(20, 12)}";
            }
        }
    }
}