using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinSystemCtl.Core;
using WinSystemCtl.Core.Data;
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
        private const int UPDATE_LOG_INTERVAL = 200; // ms
        private System.Threading.Tasks.Task? _updateLogTimer;
        private bool _stopUpdateLog;
        private bool _isNewDataAvailable = false;
        private StringBuilder _updateLogBuffer;
        private object _bufferLocker;

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

            _stopUpdateLog = true;
            _updateLogTimer?.Wait();
            _updateLogTimer?.Dispose();
            _updateLogTimer = null;
        }

        private void onTaskFinished(object sender)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Message = App.GetString("lang_TaskInfoFinishedMessage");
                State = TaskState.Finished;
            });
            _stopUpdateLog = true;
            _updateLogTimer?.Wait();
            _updateLogTimer?.Dispose();
            _updateLogTimer = null;
        }

        private void updateLogTextTask()
        {
            while (!_stopUpdateLog)
            {
                if (_isNewDataAvailable)
                {

                    MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
                    {
                        lock (_bufferLocker)
                        {
                            Log = _updateLogBuffer.ToString();
                        }
                    });
                    _isNewDataAvailable = false;
                }
                System.Threading.Thread.Sleep(UPDATE_LOG_INTERVAL);
            }
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                lock (_bufferLocker)
                {
                    Log = _updateLogBuffer.ToString();
                }
            });
        }

        private void onProcessOutput(object sender, ProcessOutputEventArgs e)
        {
            lock (_bufferLocker)
            {
                _updateLogBuffer.Append(e.Data + Environment.NewLine);
                if (_updateLogBuffer.Length > Settings.Instance.LogBufferSize)
                {
                    _updateLogBuffer.Remove(0, _updateLogBuffer.Length - Settings.Instance.LogBufferSize);
                }
            }
            _isNewDataAvailable = true;
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

            _updateLogBuffer.Clear();
            _exec.Execute();
            _stopUpdateLog = false;
            _updateLogTimer = System.Threading.Tasks.Task.Run(updateLogTextTask);

            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                State = TaskState.Running;
                Log = string.Empty;
            });
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

            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                State = TaskState.None;
                Message = string.Format(App.GetString("lang_ProcessNotInitialized"), Task.Name);
                _exec?.Dispose();
                _exec = null;
                Log = string.Empty;
            });
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

            _updateLogBuffer = new StringBuilder();
            _bufferLocker = new object();
        }

        ~TaskInfo()
        {
            _updateLogTimer?.Dispose();
            _updateLogTimer = null;
            _exec?.Dispose();
            _exec = null;
        }
    }
}
