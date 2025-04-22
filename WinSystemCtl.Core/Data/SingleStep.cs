using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WinSystemCtl.Core.Data
{
    /// <summary>
    /// Specifies the type of input/output of each single step
    /// <list type="bullet">File: Will read from file or write to file.</list>
    /// <list type="bullet">Managed: Will be redirected from/to the app.</list>
    /// </summary>
    public enum IOType
    {
        File,
        Managed,
    }

    /// <summary>
    /// Class for single step in a _task
    /// It's a single command line call or call certain program...
    /// </summary>
    public class SingleStep : ViewModelBase
    {
        private string? program;

        /// <summary>
        /// <para>Program to be executed</para>
        /// <para>RULES: If program is empty or null, then command line will be executed</para>
        /// <para>Otherwise, the program will be executed with command line as arguments</para>
        /// </summary>
        [DefaultValue(null)]
        public string? Program
        {
            get => program;
            set => Set(ref program, value);
        }

        private ObservableCollection<string>? commandLines;

        /// <summary>
        /// Command lines to be executed
        /// </summary>
        [DefaultValue(null)]
        public ObservableCollection<string>? CommandLines
        {
            get => commandLines;
            set => Set(ref commandLines, value);
        }

        private string? workingDirectory;

        /// <summary>
        /// <para> Working directory of the program </para>
        /// <para> If null or empty, use app's working dir </para>
        /// </summary>
        [DefaultValue(null)]
        public string? WorkingDirectory
        {
            get => workingDirectory;
            set => Set(ref workingDirectory, value);
        }

        private bool useCmd;

        /// <summary>
        /// <para>Whether to use command line or program, or execute the program directly</para>
        /// <para>NOTICE: If `ExportEnvironment` is enabled, you must set UseCmd=true</para>
        /// <para>Default: true</para>
        /// </summary>
        [DefaultValue(true)]
        public bool UseCmd
        {
            get => useCmd;
            set => Set(ref useCmd, value);
        }

        private bool exportEnvironment;

        /// <summary>
        /// Whether to export all environment variables after executing this step. Default: false
        /// </summary>
        [DefaultValue(false)]
        public bool ExportEnvironment
        {
            get => exportEnvironment;
            set => Set(ref exportEnvironment, value);
        }

        private bool inheritEnvironment;

        /// <summary>
        /// If enabled, the environment variables will be inherited from the last step. Default: false
        /// </summary>
        [DefaultValue(false)]
        public bool InheritEnvironment
        {
            get => inheritEnvironment;
            set => Set(ref inheritEnvironment, value);
        }

        private ObservableCollection<EnvironmentVarPair>? environmentVariables;

        /// <summary>
        /// <para> Environment variables to be passed to the program </para>
        /// <para> INFO: Orders to process environment variables (precedence): </para>
        /// <list type="bullet"> Setup initially </list>
        /// <list type="bullet"> Inherited from last step (if enabled) </list>
        /// <list type="bullet"> Set by this dictionary </list>
        /// </summary>
        [JsonConverter(typeof(EnvironmentVarPairJsonConverter))]
        public ObservableCollection<EnvironmentVarPair>? EnvironmentVariables
        {
            get => environmentVariables;
            set => Set(ref environmentVariables, value);
        }

        private IOType inputType;

        /// <summary>
        /// <para> Type of standard input </para>
        /// <list type="bullet">File: The `input` will be file path.</list>
        /// <list type="bullet">Managed: The `input` will be raw string (with some macros).</list>
        /// <para> Default: Managed </para>
        /// </summary>
        [DefaultValue(IOType.Managed)]
        public IOType InputType
        {
            get => inputType;
            set => Set(ref inputType, value);
        }

        private string? input;

        /// <summary>
        /// Specifies the standard output
        /// </summary>
        [DefaultValue(null)]
        public string? Input
        {
            get => input;
            set => Set(ref input, value);
        }

        private IOType outputType;

        /// <summary>
        /// <para> Type of standard output </para>
        /// <list type="bullet">File: The `output` will be file path.</list>
        /// <list type="bullet">Managed: The `output` will be managed and filled by app.</list>
        /// <para> In `Target` step, you cannot select `File` to be output type because app monitors the output. </para>
        /// <para> The stdout will be managed by app and `output` will not be filled </para>
        /// <para> Default: Managed </para>
        /// </summary>
        [DefaultValue(IOType.Managed)]
        public IOType OutputType
        {
            get => outputType;
            set => Set(ref outputType, value);
        }

        private string? output;

        /// <summary>
        /// Specifies the standard output
        /// </summary>
        [DefaultValue(null)]
        public string? Output
        {
            get => output;
            set => Set(ref output, value);
        }

        private bool usePseudoConsole;

        /// <summary>
        /// <para> Whether to use pseudo console </para>
        /// <para> Set this option to true to have your target process use line buffering strategy </para>
        /// <para> To enable real-time acquisition of the target process's output, the target process must use at least a line buffering strategy </para>
        /// <para> Alternatively, you may need to manually configure the buffering strategy of the target process to achieve real-time output. </para>
        /// <para> Default: true </para>
        /// </summary>
        [DefaultValue(true)]
        public bool UsePseudoConsole
        {
            get => usePseudoConsole;
            set => Set(ref usePseudoConsole, value);
        }

        public void UpdateInplace(SingleStep other)
        {
            Program = other.Program;
            CommandLines = other.CommandLines != null ? [.. other.CommandLines] : new();
            WorkingDirectory = other.WorkingDirectory;
            UseCmd = other.UseCmd;
            ExportEnvironment = other.ExportEnvironment;
            InheritEnvironment = other.InheritEnvironment;
            EnvironmentVariables = other.EnvironmentVariables != null ? [.. other.EnvironmentVariables] : new();
            InputType = other.InputType;
            Input = other.Input;
            OutputType = other.OutputType;
            Output = other.Output;
            UsePseudoConsole = other.UsePseudoConsole;
        }
        public void Reset()
        {
            Program = null;
            CommandLines = [];
            WorkingDirectory = null;
            UseCmd = true;
            ExportEnvironment = false;
            InheritEnvironment = false;
            EnvironmentVariables = [];
            InputType = IOType.Managed;
            Input = null;
            OutputType = IOType.Managed;
            Output = null;
            UsePseudoConsole = true;
        }
    }
}
