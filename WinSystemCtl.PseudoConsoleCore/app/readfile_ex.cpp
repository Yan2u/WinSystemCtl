#include <windows.h>
#include <iostream>
#include <string>
#include <atomic>
#include <winnt.h>
#include <winuser.h>

#include <thread>
#include <mutex>
#include <chrono>
#include <random>

std::string genRandomPipeName() {
    // Generate a radom pipe name
    std::mt19937_64 gen(std::random_device{}());
    std::uniform_int_distribution<> dist(0, 25);
    std::string pipeName = "\\\\.\\pipe\\mypipe_";
    for (int i = 0; i < 8; ++i) {
        char c = 'A' + dist(gen);
        pipeName += c;
    }
    return pipeName;
}

void printLastError() {
    DWORD error = GetLastError();
    LPVOID lpMsgBuf;
    FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                   NULL, error, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&lpMsgBuf, 0, NULL);
    std::wcout << L"Error: " << (LPSTR)lpMsgBuf << std::endl;
    LocalFree(lpMsgBuf);
}

struct PseudoPipe {
    std::string name;
    HANDLE read;
    HANDLE write;

    void close() {
        if (read != INVALID_HANDLE_VALUE) {
            CloseHandle(read);
            read = INVALID_HANDLE_VALUE;
        }
        if (write != INVALID_HANDLE_VALUE) {
            CloseHandle(write);
            write = INVALID_HANDLE_VALUE;
        }
    }

    PseudoPipe() {
        name = genRandomPipeName();
        read = CreateNamedPipe(
            name.c_str(),
            PIPE_ACCESS_INBOUND | FILE_FLAG_OVERLAPPED,
            PIPE_TYPE_BYTE | PIPE_WAIT,
            1,
            0, 0, 0, NULL);
        if (read == INVALID_HANDLE_VALUE) {
            printLastError();
            exit(1);
        }
        write = CreateFile(
            name.c_str(),
            GENERIC_WRITE,
            0,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
            NULL);
        if (write == INVALID_HANDLE_VALUE) {
            printLastError();
            exit(1);
        }
        // if (!CreatePipe(&read, &write, NULL, 0)) {
        //     printLastError();
        //     exit(1);
        // }
    }
};

HPCON setupPipes(const PseudoPipe &input, const PseudoPipe &output) {
    HPCON hpc;
    COORD size = {30, 90};
    HRESULT hr = CreatePseudoConsole(size, input.read, output.write, 0, &hpc);
    if (FAILED(hr)) {
        printLastError();
        exit(1);
    }
    return hpc;
}

STARTUPINFOEX genStartupInfo(HPCON hpc) {
    STARTUPINFOEX si;
    ZeroMemory(&si, sizeof(si));
    si.StartupInfo.cb = sizeof(STARTUPINFOEX);

    // Discover the size required for the list
    size_t bytesRequired;
    InitializeProcThreadAttributeList(NULL, 1, 0, &bytesRequired);

    si.lpAttributeList = (PPROC_THREAD_ATTRIBUTE_LIST)HeapAlloc(GetProcessHeap(), 0, bytesRequired);
    if (!si.lpAttributeList) {
        printLastError();
        exit(1);
    }

    if (!InitializeProcThreadAttributeList(si.lpAttributeList, 1, 0, &bytesRequired)) {
        HeapFree(GetProcessHeap(), 0, si.lpAttributeList);
        printLastError();
        exit(1);
    }

    if (!UpdateProcThreadAttribute(si.lpAttributeList,
                                   0,
                                   PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                                   hpc,
                                   sizeof(hpc),
                                   NULL,
                                   NULL)) {
        HeapFree(GetProcessHeap(), 0, si.lpAttributeList);
        printLastError();
        exit(1);
    }

    return si;
}

std::atomic_bool isProcessExited{false};

void CALLBACK onWaitForSingleObject(PVOID lpParameter, BOOLEAN bTimerOrEvent) {
    // This function is called when the process exits
    // You can use this to clean up resources or notify other parts of your program
    std::cout << "Process exited" << std::endl;
    isProcessExited = true;
}

struct ReadFileContext {
    char *buffer;
    size_t bufferSize;
    HANDLE hFile;
};

void CALLBACK onReadFileEx(DWORD dwError, DWORD dwNBytesRead, LPOVERLAPPED lpOverlapped) {
    if (dwError != ERROR_SUCCESS) {
        std::cout << "Error Code: " << dwError << std::endl;
        return;
    }

    if (isProcessExited) {
        return;
    }

    if (dwNBytesRead > 0) {
        ReadFileContext *pctx = reinterpret_cast<ReadFileContext *>(lpOverlapped->hEvent);
        char *buffer = pctx->buffer;
        std::cout.flush();
        std::cout.write(buffer, dwNBytesRead);
        std::cout << std::endl;
        std::cout.flush();

        // continue read
        ZeroMemory(buffer, pctx->bufferSize * sizeof(char));
        ReadFileEx(pctx->hFile, buffer, pctx->bufferSize, lpOverlapped, onReadFileEx);
    }
}

int main() {
    std::string commandLine = "C:\\Windows\\System32\\cmd.exe /c python test2.py";
    PseudoPipe input, output;
    HPCON hpc = setupPipes(input, output);
    STARTUPINFOEX si = genStartupInfo(hpc);
    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));
    SECURITY_ATTRIBUTES sa;
    ZeroMemory(&sa, sizeof(sa));
    sa.nLength = sizeof(sa);
    sa.bInheritHandle = TRUE;

    OVERLAPPED overlapped;
    ZeroMemory(&overlapped, sizeof(overlapped));
    char buffer[1024] = {0};
    ReadFileContext ctx{
        buffer,
        sizeof(buffer),
        output.read};
    overlapped.hEvent = reinterpret_cast<ReadFileContext *>(&ctx);

    // Create the process
    if (!CreateProcess(NULL,                                                      // No module name (use command line)
                       const_cast<char *>(commandLine.c_str()),                   // Command line
                       NULL,                                                      // Process handle not inheritable
                       NULL,                                                      // Thread handle not inheritable
                       FALSE,                                                     // Set handle inheritance to FALSE
                       EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, // No creation flags
                       NULL,                                                      // Use parent's environment block
                       "D:\\Library\\Desktop",                                    // Use parent's starting directory
                       &si.StartupInfo,                                           // Pointer to STARTUPINFO structure
                       &pi)                                                       // Pointer to PROCESS_INFORMATION structure
    ) {
        printLastError();
        exit(1);
    }

    HANDLE hWait = INVALID_HANDLE_VALUE;

    RegisterWaitForSingleObject(
        &hWait, pi.hProcess, onWaitForSingleObject, &ctx, INFINITE, WT_EXECUTEONLYONCE);

    std::cout << "started read output..." << std::endl;
    ReadFileEx(output.read, buffer, sizeof(buffer), &overlapped, onReadFileEx);
    while (!isProcessExited) {
        std::cout << "Before Sleep..." << std::endl;
        SleepEx(100, TRUE);
        std::cout << "After Sleep..." << std::endl;
    }

    UnregisterWait(hWait);
    hWait = INVALID_HANDLE_VALUE;

    CloseHandle(pi.hThread);  // Close the thread handle
    CloseHandle(pi.hProcess); // Close the process handle
    ClosePseudoConsole(hpc);
    std::cout << "hpc close..." << std::endl;
    hpc = INVALID_HANDLE_VALUE;
    CloseHandle(output.write);
    output.write = INVALID_HANDLE_VALUE;
    CloseHandle(input.read);
    input.read = INVALID_HANDLE_VALUE;

    SleepEx(0, TRUE);
    std::cout << "readFileThrad finished, Cancel Async IO..." << std::endl;
    CancelIoEx(output.read, &overlapped);

    input.close();
    output.close();
    return 0;
}