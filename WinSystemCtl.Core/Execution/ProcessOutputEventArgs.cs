using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.Execution
{
    public class ProcessOutputEventArgs : EventArgs
    {
        public string? Data { get; }

        public int Id { get; }

        public ProcessOutputEventArgs(string? data, int id)
        {
            Data = data;
            Id = id;
        }
    }
}
