#include "Utils.hpp"
#include "TaskManager.hpp"

#define DLL_EXPORT extern "C" __declspec(dllexport)

typedef void (*OutputEventHandler)(DWORD, BYTE *);
typedef void (*ExitEventHandler)(DWORD);

std::shared_ptr<TaskManager> manager = nullptr;

DLL_EXPORT void Initialize() {
    std::locale::global(std::locale(""));
    manager = std::make_shared<TaskManager>();
}

DLL_EXPORT void InitializeDetail(int bufferSize, SHORT consoleWidth, SHORT consoleHeight) {
    std::locale::global(std::locale(""));
    COORD consoleSize{consoleWidth, consoleHeight};
    manager = std::make_shared<TaskManager>(bufferSize, consoleSize);
}

DLL_EXPORT void Dispose() {
    if (manager) {
        manager->Close();
        manager.reset();
    }
}

DLL_EXPORT void RegisterErrorCallback(Utils::ErrorCallback callback) {
    Utils::UseErrorCallback(callback);
}

DLL_EXPORT int SubmitTask(const wchar_t *appName,
                          const wchar_t *commandLines,
                          const wchar_t *envVars,
                          int envLength,
                          const wchar_t *workingDir,
                          OutputEventHandler outputCallback,
                          ExitEventHandler exitCallback) {
    if (manager == nullptr) {
        return -1;
    }
    std::wstring appNameStr(appName != nullptr ? appName : L"");
    std::wstring commandLinesStr(commandLines != nullptr ? commandLines : L"");
    std::wstring workingDirStr(workingDir != nullptr ? workingDir : L"");
    std::wstring envVarsStr;
    if (envVars != nullptr && envLength > 0) {
        envVarsStr = std::wstring(envVars, envVars + envLength);
    } else {
        envVarsStr = L"";
    }
    return manager->Submit(appNameStr, commandLinesStr, envVarsStr, workingDirStr, outputCallback, exitCallback);
}

DLL_EXPORT void WriteData(int taskId, const BYTE *buffer, DWORD size) {
    if (manager == nullptr) {
        return;
    }
    manager->Write(taskId, buffer, size);
}

DLL_EXPORT void SetOutputCallback(int taskId, OutputEventHandler callback) {
    if (manager == nullptr) {
        return;
    }
    manager->SetOutputCallback(taskId, callback);
}

DLL_EXPORT void SetExitCallback(int taskId, ExitEventHandler callback) {
    if (manager == nullptr) {
        return;
    }
    manager->SetExitCallback(taskId, callback);
}

DLL_EXPORT DWORD GetTaskProcessId(int taskId) {
    if (manager == nullptr) {
        return 0;
    }
    return manager->GetProcessId(taskId);
}

DLL_EXPORT bool IsRunning(int taskId) {
    if (manager == nullptr) {
        return false;
    }
    return manager->IsRunning(taskId);
}

DLL_EXPORT void StopTask(int taskId) {
    if (manager == nullptr) {
        return;
    }
    manager->Kill(taskId);
}