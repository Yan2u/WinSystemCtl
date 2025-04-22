using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core;
using WinSystemCtl.Core.Execution;

namespace WinSystemCtl.Data
{
    public enum TaskState
    {
        None,
        Running,
        Finished,
        Error
    }

    public class TaskInfo : ViewModelBase
    {
        private Core.Data.Task _task;
        public Core.Data.Task Task
        {
            get => _task;
            set => Set(ref _task, value);
        }

        private TaskState _state;

        [JsonIgnore]
        public TaskState State
        {
            get => _state;
            set => Set(ref _state, value);
        }

        private string? _message;

        [JsonIgnore]
        public string? Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        private string? _log;

        [JsonIgnore]
        public string? Log
        {
            get => _log;
            set => Set(ref _log, value);
        }

        private bool _isScheduled;

        public bool IsScheduled
        {
            get => _isScheduled;
            set => Set(ref _isScheduled, value);
        }

        private Executor? _exec;
        private bool _isResetRequested = false;

        private void onStepEnter(object sender, StepEnterEventArgs e)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Message = string.Format(App.GetString("lang_TaskInfoStepEnterMessage"), e.Index, e.Stage.ToString());
            });
        }

        private void onExecutionError(object sender, ExecutionErrorEventArgs e)
        {
            if (_isResetRequested)
            {
                _isResetRequested = false;
                return;
            }
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Message = string.Format(App.GetString("lang_TaskInfoExecutionErrorMessage"), e.Message);
                State = TaskState.Error;
            });
        }

        private void onTaskFinished(object sender)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Message = App.GetString("lang_TaskInfoFinishedMessage");
                State = TaskState.Finished;
            });
        }

        private void onProcessOutput(object sender, ProcessOutputEventArgs e)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Log += e.Data + Environment.NewLine;
                if (Log.Length > Settings.Instance.LogBufferSize)
                {
                    Log = Log.Substring(Log.Length - Settings.Instance.LogBufferSize);
                }
            });
        }

        public void Start()
        {
            if (_exec != null)
            {
                if (_exec.Running)
                {
                    Utils.SendToast(
                        App.GetString("lang_Warning"),
                        string.Format(App.GetString("lang_ProcessAlreadyRunning"), Task.Name),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        true);
                    return;
                }
                _exec.Dispose();
                _exec = null;
            }
            _exec = new Executor(Task, new());
            _exec.OnStepEnter += onStepEnter;
            _exec.OnExecutionError += onExecutionError;
            _exec.OnTaskFinished += onTaskFinished;
            _exec.OnProcessOutput += onProcessOutput;

            State = TaskState.Running;
            Log = string.Empty;
            _exec.Execute();
        }

        public void Stop()
        {
            if (_exec == null || !_exec.Running)
            {
                Utils.SendToast(
                        App.GetString("lang_Warning"),
                        string.Format(App.GetString("lang_ProcessNotInitialized"), Task.Name),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        true);
                return;
            }
            if (_exec.HasCalledStop)
            {
                Utils.SendToast(
                        App.GetString("lang_Warning"),
                        string.Format(App.GetString("lang_ProcessHasCalledStop"), Task.Name),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        true);
                return;
            }
            _exec.Stop();
        }

        public void Reset()
        {
            if (_exec != null && _exec.Running)
            {
                if (_isResetRequested)
                {
                    Utils.SendToast(
                        App.GetString("lang_Warning"),
                        string.Format(App.GetString("lang_ProcessHasCalledReset"), Task.Name),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                        true);
                    return;
                }
                _exec.Stop();
                _isResetRequested = true;
            }

            State = TaskState.None;
            Message = string.Format(App.GetString("lang_ProcessNotInitialized"), Task.Name);
            _exec?.Dispose();
            _exec = null;
            Log = string.Empty;
        }

        public void Send(string text)
        {
            if (_exec == null || !_exec.Running)
            {
                Utils.SendToast(
                    App.GetString("lang_Warning"),
                    string.Format(App.GetString("lang_ProcessNotInitialized"), Task.Name),
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                    true);
                return;
            }
            _exec.Proc?.WriteData(text);
        }

        public TaskInfo(Core.Data.Task task)
        {
            Task = task;
            State = TaskState.None;
            Message = string.Format(App.GetString("lang_ProcessNotInitialized"), Task.Name);
            Log = string.Empty;
        }

        ~TaskInfo()
        {
            _exec?.Dispose();
            _exec = null;
        }
    }
}
