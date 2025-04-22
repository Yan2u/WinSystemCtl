using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.Execution
{
    public class ExecutionErrorEventArgs : EventArgs
    {
        public ExecutionErrorType Type { get; }
        public string? Message { get; }

        public ExecutionErrorEventArgs(ExecutionErrorType type, string? message) {
            Type = type;
            Message = message;
        }
    }
}
