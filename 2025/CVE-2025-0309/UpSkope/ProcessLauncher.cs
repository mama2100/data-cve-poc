using System;
using System.Text;
using System.Runtime.InteropServices;

namespace UpSkope
{
    public class ProcessLauncher
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("userenv.dll", SetLastError = true)]
        internal static extern bool CreateEnvironmentBlock(
            out IntPtr lpEnvironment,
            IntPtr hToken,
            bool bInherit);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        private const uint CREATE_SUSPENDED = 0x00000004;

        internal static PROCESS_INFORMATION LaunchProcess(string path, bool suspended = false)
        {
            STARTUPINFO sInfo = new STARTUPINFO();
            sInfo.cb = Marshal.SizeOf(sInfo);

            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);

            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            tSec.nLength = Marshal.SizeOf(tSec);


            uint dwCreationFlags = 0;

            if (suspended)
                dwCreationFlags |= CREATE_SUSPENDED;

            PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();

            if (CreateEnvironmentBlock(out IntPtr envBlock, IntPtr.Zero, false))
            {
                dwCreationFlags |= 0x00000400;
            }

            if (CreateProcess(path, $"{path} -u", ref pSec, ref tSec, false, dwCreationFlags,
                envBlock, null, ref sInfo, out pInfo))
            {
                Console.WriteLine($"CreateProcess() => handle={pInfo.hProcess}, pid={pInfo.dwProcessId}");
                return pInfo;
            }
            else
            {
                Console.WriteLine($"Error launching {path}, Error = {Marshal.GetLastWin32Error()}");
            }
            return pInfo;
        }
    }
}