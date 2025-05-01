#pragma once
#include "IOCPWorker.hpp"
#include "PseudoConsolePipe.hpp"
#include "OrderedConcurrentQueue.hpp"

#include <atomic>
#include <functional>
#include <memory>
#include <string>
#include <thread>
#include <wincontypes.h>
#include <windows.h>
#include <concurrent_queue.h>
#include <concurrent_unordered_map.h>

typedef std::function<void(DWORD, BYTE *)> TaskOutputEventHandler;
typedef std::function<void(DWORD)> TaskExitEventHandler;

class TaskManager;

class TaskContext {
  public:
    BYTE *BufferRead;
    std::shared_ptr<BYTE> BufferWrite;

    HANDLE HProcess, HThread;
    PseudoConsolePipe Input, Output;
    HANDLE HPc;
    HANDLE HWaitObjct;
    OVERLAPPED OverlappedRead, OverlappedWrite;
    std::atomic_bool IsFinished = false;
    std::atomic_bool IsRunning = false;
    std::atomic_bool CanWrite = true;
    std::atomic_bool CanKill = true;

    DWORD ExitCode = 0;

    TaskOutputEventHandler OutputCallback;
    TaskExitEventHandler ExitCallback;
    int Id;
    DWORD ProcessId = 0;

    int WriteSpecThreadId = -1;

    void Close();
    TaskContext();
    TaskContext(int bufferSize);
    ~TaskContext();
};

class TaskSubmitItem {
  public:
    std::wstring AppName;
    std::wstring CommandLine;
    std::wstring EnvVars;
    std::wstring WorkingDir;
    TaskOutputEventHandler OutputCallback;
    TaskExitEventHandler ExitCallback;
    int id;
};

class TaskWriteItem {
  public:
    int TaskId;
    std::shared_ptr<BYTE> Buffer;
    DWORD Size;
};

class WaitCallbackParam {
  public:
    TaskManager *ManagerPtr;
    int TaskId;
    friend class TaskManager;
};

class TaskManager {
  public:
    static constexpr int DEFAULT_MAX_BUFFER_SIZE = 4096;
    static constexpr int MAX_TASK_COUNT = 11;
    static constexpr COORD DEFAULT_CONSOLE_SIZE = {128, 128};

  private:
    friend class WaitCallbackParam;
    static VOID WINAPI waitForSingleObjectCallback(PVOID lpParameter, BOOLEAN bTimerOrEvent);

    concurrency::concurrent_unordered_map<int, std::shared_ptr<TaskContext>> _tasks;
    concurrency::concurrent_queue<int> _finished;
    concurrency::concurrent_queue<std::shared_ptr<TaskSubmitItem>> _submits;
    OrderedConcurrentQueue<std::shared_ptr<TaskWriteItem>> _writes[MAX_TASK_COUNT];
    IOCPWorker _readIOCP, _writeIOCP;
    int _currentTaskId = 0;
    int _currentWorkerCount = 0;
    std::atomic_bool _stopWorker{false};
    std::thread _workers[MAX_TASK_COUNT];

    HANDLE setupPipes(const PseudoConsolePipe &input, const PseudoConsolePipe &output);
    STARTUPINFOEXW genStartupInfo(HPCON hpc);

    void submitTask(int id, int taskThreadId, const std::wstring &appName, const std::wstring &commandLine,
                    const std::wstring &envVars, const std::wstring &workingDir,
                    TaskOutputEventHandler outputCallback,
                    TaskExitEventHandler exitCallback);

    int _bufferSize = DEFAULT_MAX_BUFFER_SIZE;
    COORD _consoleSize = DEFAULT_CONSOLE_SIZE;

    void readIOCPCallback(std::shared_ptr<TaskContext> ctx, DWORD bytesTransferred, DWORD errorCode);
    void writeIOCPCallback(std::shared_ptr<TaskContext> ctx, DWORD bytesTransferred, DWORD errorCode);

    void taskCleanUp(int taskId);
    void workerThread(int taskThreadId);

  public:
    TaskManager();
    TaskManager(int bufferSize, COORD consoleSize);

    int Submit(const std::wstring &appName,
               const std::wstring &commandLine,
               const std::wstring &envVars,
               const std::wstring &workingDir,
               TaskOutputEventHandler outputCallback,
               TaskExitEventHandler exitCallback);

    void Kill(int taskId);
    void SetOutputCallback(int taskId, TaskOutputEventHandler callback);
    void SetExitCallback(int taskId, TaskExitEventHandler callback);
    DWORD GetProcessId(int taskId);
    void Write(int taskId, const BYTE *buffer, DWORD size);
    bool IsRunning(int taskId);
    void Close();
};