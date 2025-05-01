using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
        private static readonly int UPDATE_LOG_INTERVAL = 200; // ms

        private bool _stopUpdateLog;
        private bool _isNewDataAvailable = false;
        private StringBuilder _updateLogBuffer;
        private object _bufferLocker;
        private System.Threading.Tasks.Task? _updateLogTask;

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

        private async System.Threading.Tasks.Task updateLog()
        {
            while (true)
            {
                if (_exec == null || !_exec.Running || _stopUpdateLog)
                {
                    break;
                }
                if (MainWindow.Instance == null || MainWindow.Instance.DispatcherQueue == null)
                {
                    break;
                }
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
                await System.Threading.Tasks.Task.Delay(UPDATE_LOG_INTERVAL);
            }
        }


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

            StopUpdateLog();
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() => { Log = _updateLogBuffer.ToString(); });
        }

        private void onTaskFinished(object sender)
        {
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                Message = App.GetString("lang_TaskInfoFinishedMessage");
                State = TaskState.Finished;
            });
            StopUpdateLog();
            MainWindow.Instance.DispatcherQueue.TryEnqueue(() => { Log = _updateLogBuffer.ToString(); });
        }

        private void onProcessOutput(object sender, ProcessOutputEventArgs e)
        {
            lock (_bufferLocker)
            {
                _updateLogBuffer.Append(e.Data);
                if (_updateLogBuffer.Length > Settings.Instance.LogBufferSize)
                {
                    _updateLogBuffer.Remove(0, _updateLogBuffer.Length - Settings.Instance.LogBufferSize);
                    _updateLogBuffer.Capacity = Settings.Instance.LogBufferSize;
                }
            }
            _isNewDataAvailable = true;
        }

        public void StartUpdateLog()
        {
            _stopUpdateLog = false;
            if (_updateLogTask == null || _updateLogTask.Status != TaskStatus.Running)
            {
                if (_updateLogTask != null)
                {
                    _updateLogTask.Wait();
                    _updateLogTask.Dispose();
                    _updateLogTask = null;
                }
                _updateLogTask = System.Threading.Tasks.Task.Factory.StartNew(updateLog);
            }
            else
            {
                MainWindow.Instance?.DispatcherQueue?.TryEnqueue(() =>
                {
                    Log = _updateLogBuffer.ToString();
                });
            }
        }

        public void StopUpdateLog()
        {
            _stopUpdateLog = true;
            _updateLogTask?.Wait();
            _updateLogTask?.Dispose();
            _updateLogTask = null;

            MainWindow.Instance?.DispatcherQueue?.TryEnqueue(() =>
            {
                lock (_bufferLocker)
                {
                    Log = _updateLogBuffer.ToString();
                }
            });
        }

        public void Start(bool startUpdateLog = false)
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
            var config = Utils.GetDefaultInstance<Config>();
            config.CacheOutputSize = Settings.Instance.CacheOutputSize;
            _exec = new Executor(Task, config);
            _exec.OnStepEnter += onStepEnter;
            _exec.OnExecutionError += onExecutionError;
            _exec.OnTaskFinished += onTaskFinished;
            _exec.OnProcessOutput += onProcessOutput;

            _updateLogBuffer.Clear();
            _exec.Execute();

            MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
            {
                State = TaskState.Running;
                Log = string.Empty;
            });
            if (startUpdateLog)
            {
                StartUpdateLog();
            }
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
            _exec?.Dispose();
            _exec = null;
        }
    }
}
