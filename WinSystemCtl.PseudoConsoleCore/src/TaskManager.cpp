#include "TaskManager.hpp"
#include "OrderedConcurrentQueue.hpp"
#include "Utils.hpp"
#include "Logger.hpp"

#include <errhandlingapi.h>
#include <format>
#include <ioapiset.h>
#include <iostream>
#include <locale>
#include <memory>
#include <minwinbase.h>
#include <minwindef.h>
#include <mutex>
#include <thread>

//
// TaskContext
//

TaskContext::TaskContext(int bufferSize) :
    Input(bufferSize), Output(bufferSize) {
    BufferWrite = nullptr;
    HProcess = NULL;
    HThread = NULL;
    HPc = NULL;
    HWaitObjct = NULL;
    ZeroMemory(&OverlappedRead, sizeof(OverlappedRead));
    ZeroMemory(&OverlappedWrite, sizeof(OverlappedWrite));
    BufferRead = new BYTE[bufferSize];
}

TaskContext::TaskContext() :
    TaskContext(TaskManager::DEFAULT_MAX_BUFFER_SIZE) {
}

void TaskContext::Close() {
    Utils::SafeCloseHandle(HProcess);
    Utils::SafeCloseHandle(HThread);
    // Utils::SafeCloseHandle(HPc);
    // Utils::SafeCloseHandle(HWaitObjct);
    Input.Close();
    Output.Close();
    if (BufferRead != nullptr) {
        delete[] BufferRead;
        BufferRead = nullptr;
    }
}

TaskContext::~TaskContext() {
    Close();
}

//
// TaskManager
//

VOID WINAPI TaskManager::waitForSingleObjectCallback(PVOID lpParameter, BOOLEAN bTimerOrEvent) {
    WaitCallbackParam *param = (WaitCallbackParam *)lpParameter;
    param->ManagerPtr->_finished.push(param->TaskId);
}

HANDLE TaskManager::setupPipes(const PseudoConsolePipe &input, const PseudoConsolePipe &output) {
    HPCON hpc;
    COORD size = _consoleSize;
    HRESULT hr = CreatePseudoConsole(size, input.hRead, output.hWrite, 0, &hpc);
    if (FAILED(hr)) {
        THROW_WIN32_ERROR();
    }
    return hpc;
}

STARTUPINFOEXW TaskManager::genStartupInfo(HPCON hpc) {
    STARTUPINFOEXW si;
    ZeroMemory(&si, sizeof(si));
    si.StartupInfo.cb = sizeof(STARTUPINFOEX);

    // Discover the size required for the list
    size_t bytesRequired;
    InitializeProcThreadAttributeList(NULL, 1, 0, &bytesRequired);

    si.lpAttributeList = (PPROC_THREAD_ATTRIBUTE_LIST)HeapAlloc(GetProcessHeap(), 0, bytesRequired);
    if (!si.lpAttributeList) {
        THROW_WIN32_ERROR();
    }

    if (!InitializeProcThreadAttributeList(si.lpAttributeList, 1, 0, &bytesRequired)) {
        HeapFree(GetProcessHeap(), 0, si.lpAttributeList);
        THROW_WIN32_ERROR();
    }

    if (!UpdateProcThreadAttribute(si.lpAttributeList,
                                   0,
                                   PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                                   hpc,
                                   sizeof(hpc),
                                   NULL,
                                   NULL)) {
        HeapFree(GetProcessHeap(), 0, si.lpAttributeList);
        THROW_WIN32_ERROR();
    }

    return si;
}

void TaskManager::readIOCPCallback(std::shared_ptr<TaskContext> ctx, DWORD bytesTransferred, DWORD errorCode) {
    if (_tasks.count(ctx->Id) == 0) {
        return;
    }
    if (errorCode != ERROR_SUCCESS) {
        if (ctx->IsFinished) {
            if (ctx->ExitCallback != nullptr) {
                ctx->ExitCallback(ctx->ExitCode);
            }
            if (_tasks.count(ctx->Id) > 0) {
                _tasks.unsafe_erase(ctx->Id);
            }
            return;
        }
    }
    if (bytesTransferred == 0) {
        return;
    }
    if (ctx->OutputCallback != nullptr) {
        ctx->OutputCallback(bytesTransferred, ctx->BufferRead);
    }
    if (ctx->IsFinished) {
        if (ctx->ExitCallback != nullptr) {
            ctx->ExitCallback(ctx->ExitCode);
        }
        if (_tasks.count(ctx->Id) > 0) {
            _tasks.unsafe_erase(ctx->Id);
        }
         return;
    }
    ZeroMemory(ctx->BufferRead, _bufferSize);
    ZeroMemory(&ctx->OverlappedRead, sizeof(OVERLAPPED));
    ctx->OverlappedRead.hEvent = NULL;
    if (ReadFile(ctx->Output.hRead, ctx->BufferRead, _bufferSize - 1, NULL, &ctx->OverlappedRead) == FALSE) {
        if (GetLastError() != ERROR_IO_PENDING) {
            THROW_WIN32_ERROR();
        }
    }
}

void TaskManager::writeIOCPCallback(std::shared_ptr<TaskContext> ctx, DWORD bytesTransferred, DWORD errorCode) {
    ctx->CanWrite = true;
}

void TaskManager::submitTask(int id, int taskThreadId, const std::wstring &appName, const std::wstring &commandLine,
                             const std::wstring &envVars, const std::wstring &workingDir,
                             TaskOutputEventHandler outputCallback,
                             TaskExitEventHandler exitCallback) {
    std::shared_ptr<TaskContext> ctx = std::make_shared<TaskContext>(_bufferSize);
    ctx->Id = id;
    ctx->WriteSpecThreadId = taskThreadId;
    _tasks.insert({ctx->Id, ctx});

    ctx->ExitCallback = exitCallback;
    ctx->OutputCallback = outputCallback;
    ctx->HPc = setupPipes(ctx->Input, ctx->Output);

    STARTUPINFOEXW si = genStartupInfo(ctx->HPc);
    _readIOCP.Bind(ctx->Output.hRead, std::bind(&TaskManager::readIOCPCallback, this, ctx, std::placeholders::_1, std::placeholders::_2));
    _writeIOCP.Bind(ctx->Input.hWrite, std::bind(&TaskManager::writeIOCPCallback, this, ctx, std::placeholders::_1, std::placeholders::_2));

    PROCESS_INFORMATION pi;
    LPCWSTR lpEnvVars = envVars.empty() ? NULL : envVars.c_str();
    LPCWSTR lpAppName = appName.empty() ? NULL : appName.c_str();
    LPCWSTR lpWorkingDir = workingDir.empty() ? NULL : workingDir.c_str();
    BOOL ret = CreateProcessW(
        lpAppName,
        const_cast<wchar_t *>(commandLine.c_str()),                // Command line
        NULL,                                                      // Process handle not inheritable
        NULL,                                                      // Thread handle not inheritable
        TRUE,                                                      // Set handle inheritance to TRUE
        EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, // No creation flags
        (LPVOID)lpEnvVars,                                         // Use parent's environment block
        lpWorkingDir,                                              // Use parent's starting directory
        &si.StartupInfo,                                           // Pointer to STARTUPINFO structure
        &pi);                                                      // Pointer to PROCESS_INFORMATION structure
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    }
    
    // Process Information
    ctx->ProcessId = pi.dwProcessId;
    ctx->HProcess = pi.hProcess;
    ctx->HThread = pi.hThread;

    // Register WaitForSingleObject
    HANDLE hWait = INVALID_HANDLE_VALUE;
    WaitCallbackParam *param = new WaitCallbackParam();
    param->ManagerPtr = this;
    param->TaskId = ctx->Id;
    ret = RegisterWaitForSingleObject(
        &hWait,
        pi.hProcess,
        &TaskManager::waitForSingleObjectCallback,
        (LPVOID)param, INFINITE, WT_EXECUTEONLYONCE);
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    }
    ctx->HWaitObjct = hWait;

    // start initial read
    ZeroMemory(ctx->BufferRead, _bufferSize);
    ZeroMemory(&ctx->OverlappedRead, sizeof(OVERLAPPED));
    ctx->OverlappedRead.hEvent = NULL;
    ReadFile(ctx->Output.hRead, ctx->BufferRead, _bufferSize - 1, NULL, &ctx->OverlappedRead);

    LOG_INFO("TaskManager::submitTask: {}", ctx->Id);

    ctx->IsRunning = true;
}

void TaskManager::taskCleanUp(int taskId) {
    if (_tasks.count(taskId) == 0) {
        return;
    }
    auto ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return;
    }
    DWORD exitCode = 0;
    BOOL ret = GetExitCodeProcess(ctx->HProcess, &exitCode);
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    }
    ret = UnregisterWait(ctx->HWaitObjct);
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    }
    Utils::SafeCloseHandle(ctx->HProcess);
    Utils::SafeCloseHandle(ctx->HThread);
    ClosePseudoConsole(ctx->HPc);
    std::this_thread::sleep_for(std::chrono::milliseconds(5));
    DWORD bytesAvail = 0;
    ret = PeekNamedPipe(ctx->Output.hRead, NULL, 0, NULL, &bytesAvail, NULL);
    while (ret == TRUE && bytesAvail != 0) {
        std::this_thread::sleep_for(std::chrono::milliseconds(5));
        ret = PeekNamedPipe(ctx->Output.hRead, NULL, 0, NULL, &bytesAvail, NULL);
    }
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    }
    ctx->IsFinished = true;
    ctx->IsRunning = false;
    ret = CancelIoEx(ctx->Output.hRead, &ctx->OverlappedRead);
    if (ret == FALSE) {
        DWORD code = GetLastError();
        if (code != ERROR_NOT_FOUND) {
            THROW_WIN32_ERROR();
        }
    }
    ret = CancelIoEx(ctx->Input.hWrite, &ctx->OverlappedWrite);
    if (ret == FALSE) {
        DWORD code = GetLastError();
        if (code != ERROR_NOT_FOUND) {
            THROW_WIN32_ERROR();
        }
    }
    ctx->ExitCode = exitCode;
    Utils::SafeCloseHandle(ctx->Output.hWrite);
    Utils::SafeCloseHandle(ctx->Input.hRead);
     //_tasks.unsafe_erase(taskId);
     LOG_INFO("TaskManager::taskCleanUp: {}", taskId);
}

void TaskManager::workerThread(int taskThreadId) {
    std::shared_ptr<TaskSubmitItem> submit = nullptr;
    std::shared_ptr<TaskWriteItem> write = nullptr;
    auto &&writeQueue = _writes[taskThreadId];
    int id;
    DWORD nBytes, errCode;
    BOOL ret = FALSE;
    while (!_stopWorker) {
        std::this_thread::sleep_for(std::chrono::milliseconds(10));

        // handle submit
        if (_submits.try_pop(submit) && submit != nullptr) {
            submitTask(submit->id,
                       taskThreadId,
                       submit->AppName,
                       submit->CommandLine,
                       submit->EnvVars,
                       submit->WorkingDir,
                       submit->OutputCallback,
                       submit->ExitCallback);
        }

        // handle clean up
        if (_finished.try_pop(id)) {
            taskCleanUp(id);
        }

        // handle write
        if (writeQueue.FrontNonblocking(write)
            && write != nullptr
            && _tasks.count(write->TaskId) > 0) {
            auto &&ctx = _tasks.at(write->TaskId);
            if (ctx == nullptr
                || ctx->IsFinished) {
                continue;
            }
            if (ctx->CanWrite) {
                if (!HasOverlappedIoCompleted(&ctx->OverlappedWrite)) {
                    continue;
                }
                ZeroMemory(&ctx->OverlappedWrite, sizeof(OVERLAPPED));
                ctx->OverlappedWrite.hEvent = NULL;
                // ignore this error
                WriteFile(ctx->Input.hWrite, write->Buffer.get(), write->Size, NULL, &ctx->OverlappedWrite);
                ctx->BufferWrite = write->Buffer;
                ctx->CanWrite = false;
                writeQueue.PopNoreturn();
                LOG_INFO("TaskManager::workerThread {}: WriteFile: {} bytes", taskThreadId, write->Size);
            }
        }
    }
}

void TaskManager::Close() {
    _stopWorker = true;
    for (int i = 0; i < _currentWorkerCount; ++i) {
        if (_workers[i].joinable()) {
            _workers[i].join();
        }
    }
    _readIOCP.Close();
    _writeIOCP.Close();
}

int TaskManager::Submit(const std::wstring &appName,
                        const std::wstring &commandLine,
                        const std::wstring &envVars,
                        const std::wstring &workingDir,
                        TaskOutputEventHandler outputCallback,
                        TaskExitEventHandler exitCallback) {
    auto item = std::make_shared<TaskSubmitItem>();
    item->AppName = appName;
    item->CommandLine = commandLine;
    item->OutputCallback = outputCallback;
    item->ExitCallback = exitCallback;
    item->EnvVars = envVars;
    item->WorkingDir = workingDir;
    item->id = _currentTaskId;
    ++_currentTaskId;
    if (_currentWorkerCount < MAX_TASK_COUNT) {
        _workers[_currentWorkerCount] = std::thread(&TaskManager::workerThread, this, _currentWorkerCount);
        ++_currentWorkerCount;
    }
    _submits.push(item);
    return item->id;
}

void TaskManager::Write(int taskId, const BYTE *buffer, DWORD size) {
    if (_tasks.count(taskId) == 0) {
        return;
    }
    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished || ctx->WriteSpecThreadId == -1) {
        return;
    }
    for (DWORD i = 0; i < size; i += _bufferSize) {
        DWORD writeSize = size - i > _bufferSize ? _bufferSize : size - i;
        auto writeItem = std::make_shared<TaskWriteItem>();
        writeItem->TaskId = taskId;
        BYTE *bufferCopy = new BYTE[writeSize];
        memcpy(bufferCopy, buffer + i, writeSize);
        writeItem->Buffer = std::shared_ptr<BYTE>(bufferCopy, [](BYTE *ptr) { delete[] ptr; });
        writeItem->Size = writeSize;
        _writes[ctx->WriteSpecThreadId].Push(writeItem);
    }
}

bool TaskManager::IsRunning(int taskId) {
    if (_tasks.count(taskId) == 0) {
        return false;
    }
    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return false;
    }
    return ctx->IsRunning;
}

DWORD TaskManager::GetProcessId(int taskId) {
    if (_tasks.count(taskId) == 0) {
        return 0;
    }
    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return 0;
    }
    return ctx->ProcessId;
}

void TaskManager::Kill(int taskId) {
    if (_tasks.count(taskId) == 0) {
        return;
    }

    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return;
    }

    if (!ctx->CanKill) {
        return;
    }

    BOOL ret = TerminateProcess(ctx->HProcess, -1);
    if (ret == FALSE) {
        THROW_WIN32_ERROR();
    } else {
        ctx->CanKill = false;
    }
}

void TaskManager::SetOutputCallback(int taskId, TaskOutputEventHandler callback) {
    if (_tasks.count(taskId) == 0) {
        return;
    }
    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return;
    }
    ctx->OutputCallback = callback;
}

void TaskManager::SetExitCallback(int taskId, TaskExitEventHandler callback) {
    if (_tasks.count(taskId) == 0) {
        return;
    }
    auto &&ctx = _tasks.at(taskId);
    if (ctx == nullptr || ctx->IsFinished) {
        return;
    }
    ctx->ExitCallback = callback;
}

TaskManager::TaskManager(int bufferSize, COORD consoleSize) :
    TaskManager::TaskManager() {
    _bufferSize = bufferSize;
    _consoleSize = consoleSize;
}

TaskManager::TaskManager() {
}