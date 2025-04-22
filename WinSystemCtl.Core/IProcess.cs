using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core
{
    /// <summary>
    /// Interface for process execution. <br/>
    /// This IProcess merges stdout and stderr into a single stream. <br/>
    /// For compatibility with the PseudoConsole. <br/>
    /// </summary>
    public interface IProcess : IDisposable
    {
        public delegate void DataOutputEventHandler(string? data);
        public event DataOutputEventHandler OnOutputDataReceived;

        public int Id { get; }
        public int ExitCode { get; }
        public bool HasExited { get; }

        public void Start();

        public void Kill();

        public void WriteData(string? data);

        public Task WriteDataAsync(string? data);

        public void WaitForExit();

        public Task WaitForExitAsync();
    }
}
