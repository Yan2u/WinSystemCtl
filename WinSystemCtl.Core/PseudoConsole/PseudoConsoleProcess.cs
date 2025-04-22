using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinSystemCtl.Core.Execution;
using Api = WinSystemCtl.Core.PseudoConsole.PseudoConsoleApi;

namespace WinSystemCtl.Core.PseudoConsole
{
    /// <summary>
    /// PesudoConsole Class, to solve realtime monitoring of process output.
    /// </summary>
    public partial class PseudoConsoleProcess : IDisposable, IProcess
    {
        private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const uint PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
        private const uint INFINITE = 0xFFFFFFFF;
        private const string ANSI_REGEX_STR = @"\x1b(\[.*?[@-~]|\].*?(\x07|\x1b\\))";
        private const int READ_OUTPUT_BUFFER_SIZE = 1024;

        private readonly ProcessStartInfo _startInfo;
        private nint _hPC;
        private Api.PROCESS_INFORMATION _processInfo;
        private bool _disposed;
        private PseudoConsolePipe _inputPipe, _outputPipe;
        private Task _waitForExitTask;
        private Task _readOutputDataTask;
        private Encoding _encoding;

        private bool _hasExited;
        private int _exitCode = -1;

        public event IProcess.DataOutputEventHandler? OnOutputDataReceived;
        public int Id => _processInfo.dwProcessId;
        public bool HasExited => _hasExited;
        public int ExitCode => _exitCode;

        public PseudoConsoleProcess(ProcessStartInfo startInfo)
        {
            _startInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _encoding = startInfo.StandardOutputEncoding;
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_processInfo.hProcess != nint.Zero)
                throw new InvalidOperationException("Process already started");

            // 1. Create pipe
            _inputPipe = new PseudoConsolePipe();
            _outputPipe = new PseudoConsolePipe();
            createPseudoConsoleAndPipes(out _hPC, _inputPipe, _outputPipe);

            // 2. startup
            var startupInfo = configureProcessThread(_hPC);

            // 3. env
            var envBlock = buildEnvironmentBlock(_startInfo.Environment);

            // 4. crate process
            var commandLine = buildCommandLine(_startInfo);
            createProcessWithPseudoConsole(
                commandLine,
                ref startupInfo,
                envBlock,
                out _processInfo);

            // 5. read output
            _readOutputDataTask = Task.Run(readOutputData);
        }

        public void WriteData(string? data)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));
            }

            if (HasExited)
            {
                throw new InvalidOperationException("Process has exited");
            }

            // string to byte[]
            byte[] bytes = _encoding.GetBytes(data ?? string.Empty);
            if (!Api.WriteFile(this._inputPipe.Write, bytes, (uint)bytes.Length, out uint bytesWritten, (nint)0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public Task WriteDataAsync(string? data)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));
            }

            if (HasExited)
            {
                throw new InvalidOperationException("Process has exited");
            }

            return Task.Run(() =>
            {
                byte[] bytes = _encoding.GetBytes(data ?? string.Empty);
                if (!Api.WriteFile(this._inputPipe.Write, bytes, (uint)bytes.Length, out uint bytesWritten, (nint)0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            });
        }

        public void WaitForExit()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_processInfo.hProcess == nint.Zero)
                throw new InvalidOperationException("Process not started");
            if (HasExited) { return; }

            monitorProcessExit();
        }

        public Task WaitForExitAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_processInfo.hProcess == nint.Zero)
                throw new InvalidOperationException("Process not started");

            if (HasExited)
            {
                return Task.CompletedTask;
            }

            if (_waitForExitTask == null)
            {
                _waitForExitTask = Task.Run(monitorProcessExit);
            }

            return _waitForExitTask;
        }

        public void Kill()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_processInfo.hProcess == nint.Zero || HasExited)
                return;

            if (!Api.TerminateProcess(_processInfo.hProcess, -1))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free managed resources.
                    _inputPipe?.Dispose();
                    _outputPipe?.Dispose();
                }
                
                closeHandles();
                Debug.Print("Close process handles");
                if (_hPC != IntPtr.Zero) { Marshal.FreeHGlobal(_hPC); }
                Debug.Print("Process Free _hPC");
                _disposed = true;
            }
        }

        private void createPseudoConsoleAndPipes(out nint hPC, PseudoConsolePipe iPipe, PseudoConsolePipe oPipe)
        {
            Api.SECURITY_ATTRIBUTES sa = new() { nLength = Marshal.SizeOf<Api.SECURITY_ATTRIBUTES>(), bInheritHandle = true };

            Api.COORD size = new() { X = 90, Y = 30 };
            int hr = Api.CreatePseudoConsole(size, iPipe.Read, oPipe.Write, 0, out hPC);
            if (hr != 0)
                throw new Win32Exception(hr);
        }

        private Api.STARTUPINFOEX configureProcessThread(nint hPC)
        {
            var lpSize = nint.Zero;
            if (Api.InitializeProcThreadAttributeList(nint.Zero, 1, 0, ref lpSize) || lpSize == nint.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var startupInfo = new Api.STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<Api.STARTUPINFOEX>();
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);

            if (!Api.InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref lpSize))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!Api.UpdateProcThreadAttribute(
                startupInfo.lpAttributeList,
                0,
                (nint)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                hPC,
                nint.Size,
                nint.Zero,
                nint.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return startupInfo;
        }

        private nint buildEnvironmentBlock(IDictionary<string, string> environment)
        {
            if (environment == null || environment.Count == 0)
                return nint.Zero;

            var envBlock = new StringBuilder();
            foreach (var kv in environment)
            {
                envBlock.Append($"{kv.Key}={kv.Value}\0");
            }
            envBlock.Append('\0');
            return Marshal.StringToHGlobalUni(envBlock.ToString());
        }

        private string buildCommandLine(ProcessStartInfo startInfo)
        {
            if (startInfo.UseShellExecute)
                throw new NotSupportedException("UseShellExecute is not supported with PseudoConsole");

            var fileName = startInfo.FileName;
            string arguments = string.Empty;

            if (startInfo.ArgumentList.Count > 0)
            {
                arguments = string.Join(' ', startInfo.ArgumentList);
            }
            else if (!string.IsNullOrEmpty(startInfo.Arguments))
            {
                arguments = startInfo.Arguments;
            }

            if (string.IsNullOrWhiteSpace(arguments))
                return fileName;

            return $"\"{fileName}\" {arguments}";
        }

        private void createProcessWithPseudoConsole(
            string commandLine,
            ref Api.STARTUPINFOEX startupInfo,
            nint envBlock,
            out Api.PROCESS_INFORMATION processInfo)
        {
            uint flags = EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT;

            Api.SECURITY_ATTRIBUTES pSec = new() { nLength = Marshal.SizeOf<Api.SECURITY_ATTRIBUTES>() };
            Api.SECURITY_ATTRIBUTES tSec = new() { nLength = Marshal.SizeOf<Api.SECURITY_ATTRIBUTES>() };

            bool success = Api.CreateProcess(
                lpApplicationName: null,
                lpCommandLine: commandLine,
                lpProcessAttributes: ref pSec,
                lpThreadAttributes: ref tSec,
                bInheritHandles: false,
                dwCreationFlags: flags,
                lpEnvironment: envBlock,
                lpCurrentDirectory: _startInfo.WorkingDirectory,
                lpStartupInfo: ref startupInfo,
                lpProcessInformation: out processInfo);

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private void onProcessExit()
        {
            Debug.Print("Process Exited");
            Api.ClosePseudoConsole(_hPC);
            _hPC = nint.Zero;
            Debug.Print("PseudoConsole Closed");
            if (_outputPipe.Write != IntPtr.Zero)
            {
                Api.CloseHandle(_outputPipe.Write);
                _outputPipe.Write = IntPtr.Zero;
            }
            if (_inputPipe.Read != IntPtr.Zero)
            {
                Api.CloseHandle(_inputPipe.Read);
                _inputPipe.Read = IntPtr.Zero;
            }
            Debug.Print("Waiting _readOutputDataTask...");
            _readOutputDataTask.Wait();
        }

        private void monitorProcessExit()
        {
            Api.WaitForSingleObject(_processInfo.hProcess, INFINITE);
            _hasExited = true;
            if (Api.GetExitCodeProcess(_processInfo.hProcess, out int exitCode))
            {
                _exitCode = exitCode;
            }

            onProcessExit();
            Debug.Print("monitorProcessExit() finished");
        }

        private void readOutputData()
        {
            Debug.Print("readOutputData() started");
            byte[] buffer = new byte[READ_OUTPUT_BUFFER_SIZE];
            string? data = string.Empty;
            string? line = string.Empty;
            while (Api.ReadFile(_outputPipe.Read, buffer, READ_OUTPUT_BUFFER_SIZE, out uint bytesRead, (nint)0))
            {
                if (bytesRead == 0) { break; }
                data += Regex.Replace(_encoding.GetString(buffer.Take((int)bytesRead).ToArray()), ANSI_REGEX_STR, string.Empty);
                while (data.Contains(Environment.NewLine))
                {
                    line = data[..data.IndexOf(Environment.NewLine)];
                    OnOutputDataReceived?.Invoke(line);
                    data = data[(data.IndexOf(Environment.NewLine) + Environment.NewLine.Length)..];
                }
            }
            if (!string.IsNullOrEmpty(data))
            {
                while (data.Contains(Environment.NewLine))
                {
                    line = data[..data.IndexOf(Environment.NewLine)];
                    OnOutputDataReceived?.Invoke(line);
                    data = data[(data.IndexOf(Environment.NewLine) + Environment.NewLine.Length)..];
                }
                if (!string.IsNullOrEmpty(data))
                {
                    OnOutputDataReceived?.Invoke(data);
                }
            }
            Debug.Print("readOutputData() finished");
        }

        private void closeHandles()
        {
            if (_processInfo.hProcess != nint.Zero)
            {
                Api.CloseHandle(_processInfo.hProcess);
                _processInfo.hProcess = nint.Zero;
            }
            if (_processInfo.hThread != nint.Zero)
            {
                Api.CloseHandle(_processInfo.hThread);
                _processInfo.hThread = nint.Zero;
            }
        }
    }
}