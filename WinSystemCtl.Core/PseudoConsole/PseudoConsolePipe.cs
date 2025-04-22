using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.PseudoConsole
{
    public class PseudoConsolePipe : IDisposable
    {
        private IntPtr _read;
        private IntPtr _write;

        public IntPtr Read
        {
            get => _read;
            set => _read = value;
        }

        public IntPtr Write
        {
            get => _write;
            set => _write = value;
        }

        private bool _disposed = false;

        public PseudoConsolePipe()
        {
            if (!PseudoConsoleApi.CreatePipe(out _read, out _write, nint.Zero, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Read != IntPtr.Zero)
                    {
                        PseudoConsoleApi.CloseHandle(Read);
                        Read = IntPtr.Zero;
                    }
                    if (Write != IntPtr.Zero)
                    {
                        PseudoConsoleApi.CloseHandle(Write);
                        Write = IntPtr.Zero;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
