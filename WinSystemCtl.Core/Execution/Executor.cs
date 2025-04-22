using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using WinSystemCtl.Core.Data;
using WinSystemCtl.Core.PseudoConsole;


namespace WinSystemCtl.Core.Execution
{
    public delegate void StepEnterEventHandler(object sender, StepEnterEventArgs e);
    public delegate void ExecutionErrorEventHandler(object sender, ExecutionErrorEventArgs e);
    public delegate void ProcessOutputEventHandler(object sender, ProcessOutputEventArgs e);
    public delegate void TaskFinishedEventHandler(object sender);

    public class Executor : IDisposable
    {
        public event StepEnterEventHandler? OnStepEnter;
        public event ExecutionErrorEventHandler? OnExecutionError;
        public event ProcessOutputEventHandler? OnProcessOutput;
        public event TaskFinishedEventHandler? OnTaskFinished;

        public static readonly string EnvLogFileFolder = ".envlogs";

        private static readonly string wteePath = Path.Join(Environment.CurrentDirectory, "wtee.exe");

        private Data.Task _task;
        private Config _config;

        private StringBuilder _cachedOutput;
        private ObservableCollection<EnvironmentVarPair> _cachedEnv;

        private IProcess? _proc;

        private string? _envLogFilename;

        private int _currentStepIndex = -1;
        private Stage _currentStage;
        private SingleStep _currentStep;
        private int _currentProcessId;

        private CancellationTokenSource _inputReadLinesCts;
        private System.Threading.Tasks.Task _readStdOutputTask;
        private System.Threading.Tasks.Task _readStdErrorTask;
        private System.Threading.Tasks.Task? _inputWriteLineTask;
        private System.Threading.Tasks.TaskFactory _taskFactory;
        private bool _disposed;

        public bool HasCalledStop { get; set; } = false;
        public bool Running { get; set; } = false;

        public IProcess? Proc
        {
            get => _proc;
        }

        private void onReceiveStandardOutput(string? data)
        {
            if (data == null) { return; }
            if (_currentStage == Stage.Target || _config.ShowAllOutput)
            {
                OnProcessOutput?.Invoke(this, new(data, this._proc?.Id ?? -1));
            }
            _cachedOutput.Append(data + Environment.NewLine);
        }

        private ProcessStartInfo? genStartInfo(SingleStep step, bool isTarget = false)
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            // Executable path

            if (!string.IsNullOrEmpty(step.Program) && !File.Exists(step.Program))
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, $"{Properties.ExecutionError.ERR_PROGRAM_NOT_EXISTS}: {step.Program}"));
                return null;
            }

            if (step.UseCmd)
            {
                psi.FileName = "C:\\Windows\\System32\\cmd.exe";
                psi.ArgumentList.Add("/c");

                if (!string.IsNullOrEmpty(step.Program))
                {
                    psi.ArgumentList.Add($"\"{step.Program}\"");
                }
            }
            else
            {
                psi.FileName = step.Program;
            }

            // Working directory
            if (!string.IsNullOrEmpty(step.WorkingDirectory))
            {
                if (!Path.Exists(step.WorkingDirectory))
                {
                    OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, $"{Properties.ExecutionError.ERR_FOLDER_NOT_EXISTS}: {step.WorkingDirectory}"));
                    return null;
                }
                else
                {
                    psi.WorkingDirectory = step.WorkingDirectory;
                }
            }
            else
            {
                psi.WorkingDirectory = Environment.CurrentDirectory;
            }

            if (step.CommandLines != null && step.CommandLines.Count > 0)
            {
                Utils.ConcatInplace(psi.ArgumentList, step.CommandLines);
            }

            // Environment variables

            if (step.ExportEnvironment)
            {
                if (!step.UseCmd)
                {
                    OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, Properties.ExecutionError.ERR_EXPORT_ENV_CONFLICT));
                    return null;
                }

                Utils.ConcatInplace(psi.ArgumentList, ["&&", "set", "|", wteePath, _envLogFilename, ">nul"]);
            }

            if (step.InheritEnvironment)
            {
                Utils.UpdateEnvironmentVariables(psi.Environment, _cachedEnv);
            }

            if (step.EnvironmentVariables != null)
            {
                Utils.UpdateEnvironmentVariables(psi.Environment, step.EnvironmentVariables);
            }

            // Standard input or output
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;

            if (step.InputType == IOType.File && !File.Exists(step.Input))
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, $"{Properties.ExecutionError.ERR_FILE_NOT_EXISTS}: {step.Input}"));
                return null;
            }

            if (step.OutputType == IOType.File && !File.Exists(step.Output))
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, $"{Properties.ExecutionError.ERR_FILE_NOT_EXISTS}: {step.Output}"));
                return null;
            }

            // Encodings
            try
            {
                psi.StandardOutputEncoding = Encoding.GetEncoding(string.IsNullOrEmpty(_task.Encoding) ? "UTF-8" : _task.Encoding);
            }
            catch (ArgumentException)
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, $"{Properties.ExecutionError.ERR_INVALID_ENCODING}: {_task.Encoding}"));
                return null;
            }

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            return psi;
        }

        private async System.Threading.Tasks.Task inputWriteLineAsync(string inputFilePath)
        {
            try
            {
                await foreach (var line in File.ReadLinesAsync(inputFilePath).WithCancellation(_inputReadLinesCts.Token))
                {
                    _proc.WriteData(line + Environment.NewLine);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private void execSingleStepAsync(SingleStep step)
        {
            var psi = genStartInfo(step);
            if (psi == null)
            {
                Running = false;
                return;
            }

            this._proc?.Dispose();
            if (step.UsePseudoConsole)
            {
                this._proc = new PseudoConsoleProcess(psi);
            }
            else
            {
                this._proc = new ProcessWrapper(psi);
            }

            var lastOutput = _cachedOutput?.ToString() ?? null;
            _cachedOutput.Clear();

            _proc.Start();
            _currentProcessId = _proc.Id;
            _proc.OnOutputDataReceived += onReceiveStandardOutput;
            Running = true;

            if (step.InputType == IOType.File)
            {
                if (_inputReadLinesCts.TryReset())
                {
                    _inputReadLinesCts = new CancellationTokenSource();
                }
                _inputWriteLineTask = inputWriteLineAsync(step.Input);
            }
            else if (!string.IsNullOrEmpty(step.Input))
            {
                if (!string.IsNullOrEmpty(lastOutput))
                {
                    _proc.WriteData(step.Input.Replace("${pipe}", lastOutput));
                }
                else
                {
                    _proc.WriteData(step.Input);
                }
            }

            _proc.WaitForExitAsync().ContinueWith(onOneStepFinished);
        }

        private void onOneStepFinished(System.Threading.Tasks.Task _)
        {
            _readStdErrorTask?.Wait();
            _readStdOutputTask?.Wait();
            if (_inputWriteLineTask != null && _inputWriteLineTask.Status == TaskStatus.Running)
            {
                _inputReadLinesCts.Cancel();
                _inputWriteLineTask.Wait();
                _inputWriteLineTask = null;
            }

            if (_proc == null || HasCalledStop) { return; }

            Debug.Print("One Step finished");


            if (_proc.ExitCode != 0)
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.ProgramError, $"{Properties.ExecutionError.ERR_PROCESS_EXITED_UNEXPECTEDLY}: {_currentProcessId} ({_proc.ExitCode})"));
                Running = false;
                return;
            }

            if (_currentStep.ExportEnvironment)
            {
                _cachedEnv = Utils.LoadEnvironmentVars(_envLogFilename);
                if (_cachedEnv == null)
                {
                    OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, Properties.ExecutionError.ERR_EXPORT_ENVIRONMENT_FAILED));
                    Running = false;
                    return;
                }
            }

            if (_currentStage != Stage.Target && _currentStep.OutputType == IOType.File)
            {
                using (StreamWriter writer = new(_currentStep.Output, false, Encoding.GetEncoding(_task.Encoding)))
                {
                    writer.Write(_cachedOutput);
                }
            }

            switch (_currentStage)
            {
                case Stage.Preprocess:
                    if (_currentStepIndex == _task.PreprocessSteps!.Count - 1)
                    {
                        _currentStage = Stage.Target;
                        _currentStepIndex = 0;
                        _currentStep = _task.Target!;
                    }
                    else
                    {
                        ++_currentStepIndex;
                        _currentStep = _task.PreprocessSteps[_currentStepIndex];
                    }
                    break;
                case Stage.Target:
                    if (_task.PostprocessSteps != null && _task.PostprocessSteps.Count > 0)
                    {
                        _currentStage = Stage.Postprocess;
                        _currentStepIndex = 0;
                        _currentStep = _task.PostprocessSteps[_currentStepIndex];
                    }
                    else
                    {
                        OnTaskFinished?.Invoke(this);
                        Running = false;
                        return;
                    }
                    break;
                case Stage.Postprocess:
                    if (_currentStepIndex == _task.PostprocessSteps!.Count - 1)
                    {
                        OnTaskFinished?.Invoke(this);
                        Running = false;
                        return;
                    }
                    else
                    {
                        ++_currentStepIndex;
                        _currentStep = _task.PostprocessSteps[_currentStepIndex];
                    }
                    break;
            }
            OnStepEnter?.Invoke(this, new(_currentStage, _currentStepIndex, _currentStep));
            execSingleStepAsync(_currentStep);
        }

        public void Execute()
        {
            if (Running)
            {
                return;
            }

            if (_task.Target == null)
            {
                OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, Properties.ExecutionError.ERR_TARGET_NULL));
                return;
            }

            HasCalledStop = false;
            if (_task.PreprocessSteps != null && _task.PreprocessSteps.Count > 0)
            {
                Debug.Print("Preprocess start.");

                _currentStepIndex = 0;
                _currentStage = Stage.Preprocess;
                _currentStep = _task.PreprocessSteps[_currentStepIndex];

            }
            else
            {
                Debug.Print("Target start.");

                _currentStepIndex = 0;
                _currentStage = Stage.Target;
                _currentStep = _task.Target;
            }

            OnStepEnter?.Invoke(this, new(_currentStage, _currentStepIndex, _currentStep));
            execSingleStepAsync(_currentStep);
        }

        public void Stop()
        {
            if (!HasCalledStop && _proc != null && !_proc.HasExited)
            {
                HasCalledStop = true;
                _proc.Kill();
                _proc.WaitForExitAsync().ContinueWith(_ =>
                {
                    this.OnExecutionError?.Invoke(this, new(ExecutionErrorType.FlowError, Properties.ExecutionError.ERR_STOP_BY_USER));
                    this.HasCalledStop = false;
                    this._proc = null;
                    this.Running = false;
                });
            }
        }

        static Executor()
        {
            // Detect and release wtee.exe
            if (!File.Exists(wteePath))
            {
                FileStream fs = new(wteePath, FileMode.OpenOrCreate);
                byte[] bytes = Properties.Executor.WTEE;
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }

            // Make env logs path
            if (!Path.Exists(Path.Join(Environment.CurrentDirectory, EnvLogFileFolder)))
            {
                Directory.CreateDirectory(Path.Join(Environment.CurrentDirectory, EnvLogFileFolder));
            }
        }

        public Executor(Data.Task task, Config config)
        {
            this._task = task;
            this._config = config;

            this._cachedOutput = new StringBuilder();
            this._cachedEnv = new();
            this._taskFactory = new();

            this._inputReadLinesCts = new CancellationTokenSource();
            this._envLogFilename = Path.Join(Environment.CurrentDirectory, EnvLogFileFolder, Path.GetRandomFileName());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Running) { Stop(); }
                    _proc?.Dispose();
                    _proc = null;

                    if (_envLogFilename != null && File.Exists(_envLogFilename))
                    {
                        File.Delete(_envLogFilename);
                    }
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
