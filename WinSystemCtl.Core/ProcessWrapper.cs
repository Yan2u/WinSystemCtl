using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core
{
    /// <summary>
    /// Wrapper for process execution, to support IProcess interface.
    /// </summary>
    public class ProcessWrapper : IProcess
    {
        private Process? _proc;
        private bool _disposed;
        private Encoding? _encoding;

        private Task? _readOutputTask;
        private Task? _readErrorTask;

        public int Id => _proc?.Id ?? -1;
        public int ExitCode => _proc?.ExitCode ?? -1;
        public bool HasExited => _proc?.HasExited ?? true;

        public ProcessWrapper(ProcessStartInfo startInfo)
        {
            _proc = new Process() { StartInfo = startInfo };
            _encoding = startInfo.StandardOutputEncoding;
        }

        public event IProcess.DataOutputEventHandler OnOutputDataReceived;

        public void Kill()
        {
            _proc?.Kill(true);
        }

        public void Start()
        {
            if (_proc == null) { throw new InvalidOperationException("Process is not initialized."); }

            if (_proc.Start())
            {
                _readOutputTask = Task.Run(() => readProcessOutput(_proc.StandardOutput));
                _readErrorTask = Task.Run(() => readProcessOutput(_proc.StandardError));
            }
        }

        public Task WaitForExitAsync()
        {
            return Task.Run(WaitForExit);
        }

        public void WaitForExit()
        {
            if (_proc == null) { throw new InvalidOperationException("Process is disposed."); }

            _proc.WaitForExit();
            _readOutputTask?.Wait();
            _readErrorTask?.Wait();
        }

        private void readProcessOutput(StreamReader reader)
        {
            Debug.Print("readProcessOutput() started.");

            while (true)
            {
                string? line = reader.ReadLine();
                if (line == null) { break; }
                OnOutputDataReceived?.Invoke(line);
                Debug.Print("Readline()");
            }

            Debug.Print("readProcessOutput() finished.");
        }

        public void WriteData(string? data)
        {
            if (_proc == null) { throw new InvalidOperationException("Process is not initialized."); }
            if (_proc.HasExited) { throw new InvalidOperationException("Process has exited"); }

            if (_proc.StandardInput.BaseStream.CanWrite)
            {
                _proc.StandardInput.Write(data, _encoding);
            }
            else
            {
                throw new InvalidOperationException("Process StandardInput is not writable.");
            }
        }

        public Task WriteDataAsync(string? data)
        {
            return Task.Run(() => WriteData(data));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _proc.Dispose();
                    _proc = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
