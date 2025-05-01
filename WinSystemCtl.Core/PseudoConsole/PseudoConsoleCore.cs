using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.PseudoConsole
{
    public static class PseudoConsoleCore
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ErrorEventHandler(int errCode, int line, [MarshalAs(UnmanagedType.LPStr)] string file);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OutputEventHandler(int nBytes, IntPtr buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ExitEventHandler(int exitCode);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterErrorCallback(ErrorEventHandler handler);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Initialize();

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeDetail(int bufferSize, short consoleWidth, short consoleHeight);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Dispose();

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SubmitTask([MarshalAs(UnmanagedType.LPWStr)] string? appName,
                                            [MarshalAs(UnmanagedType.LPWStr)] string? commandLines,
                                            [MarshalAs(UnmanagedType.LPWStr)] string? envVars,
                                            int envLength,
                                            [MarshalAs(UnmanagedType.LPWStr)] string? workingDir,
                                            IntPtr outputCallback,
                                            IntPtr exitCallback);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetOutputCallback(int taskId, OutputEventHandler outputCallback);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetExitCallback(int taskId, ExitEventHandler exitCallback);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void WriteData(int taskId, IntPtr buffer, int size);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetTaskProcessId(int taskId);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsRunning(int taskId);

        [DllImport("libPseudoConsoleCore.dll", CharSet = CharSet.Ansi, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StopTask(int taskId);
    }
}
