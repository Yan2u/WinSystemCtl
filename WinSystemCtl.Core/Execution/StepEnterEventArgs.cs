using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core.Data;

namespace WinSystemCtl.Core.Execution
{
    public class StepEnterEventArgs : EventArgs
    {
        /// <summary>
        /// The stage where the step finished.
        /// </summary>
        public Stage Stage { get; }

        /// <summary>
        /// The index of the step in the stage.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Step that just finished.
        /// </summary>

        public SingleStep Step { get; }

        public StepEnterEventArgs(Stage stage, int index, SingleStep step)
        {
            this.Stage = stage;
            this.Index = index;
            this.Step = step;
        }
    }
}
