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

namespace WinSystemCtl.Core.PseudoConsole
{
    public delegate void DataOutputEventHandler(string? data);
    public delegate void ProcessExitEventHandler(int exitCode);

    /// <summary>
    /// PesudoConsole Class, to solve realtime monitoring of process output.
    /// </summary>
    public partial class PseudoConsoleProcess : IDisposable
    {
        private const string ANSI_REGEX_STR = @"\x1b(\[.*?[@-~]|\].*?(\x07|\x1b\\))";

        private ProcessStartInfo _startInfo;
        private bool _disposed;
        private int _processId;
        private bool _hasExited = false;
        private bool _hasStopped = false;
        private Encoding _encoding;
        private int _exitCode = -1;
        private int _taskCoreId = -1;

        private PseudoConsoleCore.OutputEventHandler? _fixedOutputCallback;
        private PseudoConsoleCore.ExitEventHandler? _fixedExitCallback;

        public event DataOutputEventHandler? OnOutputDataReceived;
        public event ProcessExitEventHandler? OnProcessExited;

        public int Id => _processId;
        public bool HasExited => _hasExited;
        public int ExitCode => _exitCode;

        public PseudoConsoleProcess(ProcessStartInfo startInfo)
        {
            _startInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _encoding = startInfo.StandardOutputEncoding ?? Encoding.UTF8;

            _fixedExitCallback = new PseudoConsoleCore.ExitEventHandler(coreExitCallback);
            _fixedOutputCallback = new PseudoConsoleCore.OutputEventHandler(coreOutputCallback);
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_taskCoreId != -1 && PseudoConsoleCore.IsRunning(_taskCoreId))
                throw new InvalidOperationException("Process already started");

            string commandLine = buildCommandLine(_startInfo);
            string envVars = buildEnvironmentBlock(_startInfo.Environment);

            

            _hasExited = false;
            _hasStopped = false;
            _taskCoreId = PseudoConsoleCore.SubmitTask(null,
                                                       commandLine,
                                                       envVars,
                                                       envVars.Length,
                                                       _startInfo.WorkingDirectory,
                                                       Marshal.GetFunctionPointerForDelegate(_fixedOutputCallback),
                                                       Marshal.GetFunctionPointerForDelegate(_fixedExitCallback));

            while (!PseudoConsoleCore.IsRunning(_taskCoreId))
            {
                Thread.Yield();
            }

            _processId = PseudoConsoleCore.GetTaskProcessId(_taskCoreId);
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

            if (!string.IsNullOrEmpty(data) && _taskCoreId != -1)
            {
                byte[] bytes = _encoding.GetBytes(data);
                int nBytes = bytes.Length;
                IntPtr buffer = Marshal.AllocHGlobal(nBytes);
                Marshal.Copy(bytes, 0, buffer, nBytes);
                PseudoConsoleCore.WriteData(_taskCoreId, buffer, nBytes);
                Marshal.FreeHGlobal(buffer);
            }
        }

        public Task WriteDataAsync(string? data)
        {
            return Task.Factory.StartNew(() => WriteData(data));
        }

        public void WaitForExit()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PseudoConsoleProcess));

            if (_taskCoreId == -1)
                throw new InvalidOperationException("Process not started");

            while (!_hasExited)
            {
                Thread.Sleep(100);
            }
        }

        public Task WaitForExitAsync()
        {
            return Task.Factory.StartNew(WaitForExit);
        }

        public void Kill()
        {
            if (_taskCoreId != -1 && !_hasStopped)
            {
                PseudoConsoleCore.StopTask(_taskCoreId);
                _hasStopped = true;
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
                    if (_taskCoreId != -1 && PseudoConsoleCore.IsRunning(_taskCoreId))
                    {
                        PseudoConsoleCore.StopTask(_taskCoreId);
                        _taskCoreId = -1;
                    }
                }

                _disposed = true;
            }
        }

        private string buildEnvironmentBlock(IDictionary<string, string> environment)
        {
            if (environment == null || environment.Count == 0)
                return string.Empty;

            var envBlock = new StringBuilder();
            foreach (var kv in environment)
            {
                envBlock.Append($"{kv.Key}={kv.Value}\0");
            }
            envBlock.Append('\0');
            return envBlock.ToString();
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

        private void coreExitCallback(int exitCode)
        {
            _hasExited = true;
            _exitCode = exitCode;
            _taskCoreId = -1;
            OnProcessExited?.Invoke(exitCode);
        }

        private void coreOutputCallback(int nBytes, IntPtr buffer)
        {
            byte[] bytes = new byte[nBytes];
            Marshal.Copy(buffer, bytes, 0, nBytes);
            string data = _encoding.GetString(bytes);
            data = Regex.Replace(data, ANSI_REGEX_STR, string.Empty);
            OnOutputDataReceived?.Invoke(data);
        }
    }
}