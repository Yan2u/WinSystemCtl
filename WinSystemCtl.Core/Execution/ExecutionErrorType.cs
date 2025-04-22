using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.Execution
{
    /// <summary>
    /// <list type="bullet">ProgramError: means program is exectued wrongly (exit code != 0)</list>
    /// <list type="bullet">FlowError: means configuration is not valid.</list>
    /// </summary>
    public enum ExecutionErrorType
    {
        ProgramError,
        FlowError
    }
}
