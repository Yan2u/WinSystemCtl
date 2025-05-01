#include <windows.h>
#include <iostream>
#include <string>
#include <atomic>
#include <winerror.h>
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

std::atomic_bool isProcess1Exited{false};
std::atomic_bool isProcess2Exited{false};

void CALLBACK onWaitForSingleObject(PVOID lpParameter, BOOLEAN bTimerOrEvent) {
    std::cout << "Process 1 exited" << std::endl;
    isProcess1Exited = true;
}

void CALLBACK onWaitForSingleObject2(PVOID lpParameter, BOOLEAN bTimerOrEvent) {
    std::cout << "Process 2 exited" << std::endl;
    isProcess2Exited = true;
}

int main() {
    std::string commandLine = "C:\\Windows\\System32\\cmd.exe /c python test2.py";
    PseudoPipe input, output;
    PseudoPipe input2, output2;
    HPCON hpc = setupPipes(input, output);
    HPCON hpc2 = setupPipes(input2, output2);
    STARTUPINFOEX si = genStartupInfo(hpc);
    STARTUPINFOEX si2 = genStartupInfo(hpc2);
    PROCESS_INFORMATION pi;
    PROCESS_INFORMATION pi2;
    ZeroMemory(&pi, sizeof(pi));
    ZeroMemory(&pi2, sizeof(pi2));

    OVERLAPPED overlapped, overlapped2;
    ZeroMemory(&overlapped, sizeof(overlapped));
    ZeroMemory(&overlapped2, sizeof(overlapped2));
    char buffer[1024] = {0};
    char buffer2[1024] = {0};

    HANDLE hIOCP = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    hIOCP = CreateIoCompletionPort(output.read, hIOCP, 0, 0);
    if (hIOCP == NULL) {
        printLastError();
        exit(1);
    }
    hIOCP = CreateIoCompletionPort(output2.read, hIOCP, 1, 0);
    if (hIOCP == NULL) {
        printLastError();
        exit(1);
    }

    auto readProcessOutputThread = [&]() {
        std::cout << "started read output..." << std::endl;
        DWORD dwBytesRead = 0;
        uint64_t completionKey = 0;
        LPOVERLAPPED lpOverlapped = NULL;
        ReadFile(output.read, buffer, sizeof(buffer) - 1, NULL, &overlapped);
        BOOL ret;
        int nTimeOut = 0;
        while (!isProcess1Exited && !isProcess2Exited) {
            while (true) {
                // std::cout << "before GetQueuedCompletionStatus..." << std::endl;
                ret = GetQueuedCompletionStatus(hIOCP, &dwBytesRead, &completionKey, &lpOverlapped, 100);
                // std::cout << "after GetQueuedCompletionStatus..." << std::endl;
                if (ret == FALSE) {
                    if (GetLastError() == WAIT_TIMEOUT) {
                        nTimeOut = min(nTimeOut + 1, 10);
                        continue;
                    } else {
                        printLastError();
                        break;
                    }
                } else {
                    nTimeOut = 0;
                    break;
                }
            }
            if (ret == FALSE) {
                break;
            }
            if (dwBytesRead == 0) {
                break;
            }
            if (completionKey == 0) {
                std::cout << "Process1 Output: ";
                std::cout.write(buffer, dwBytesRead);
                overlapped.Offset = 0;
                overlapped.OffsetHigh = 0;
                overlapped.hEvent = NULL;
                ZeroMemory(buffer, sizeof(buffer));
                ReadFile(output.read, buffer, sizeof(buffer) - 1, NULL, &overlapped);
            } else {
                std::cout << "Process2 Output: ";
                std::cout.write(buffer2, dwBytesRead);
                overlapped.Offset = 0;
                overlapped.OffsetHigh = 0;
                overlapped.hEvent = NULL;
                ZeroMemory(buffer2, sizeof(buffer2));
                ReadFile(output.read, buffer2, sizeof(buffer2) - 1, NULL, &overlapped);
            }
        }
        std::cout << "read output thread finished..." << std::endl;
    };

    // Create the process
    if (!CreateProcess(NULL,                                                      // No module name (use command line)
                       const_cast<char *>(commandLine.c_str()),                   // Command line
                       NULL,                                                      // Process handle not inheritable
                       NULL,                                                      // Thread handle not inheritable
                       TRUE,                                                      // Set handle inheritance to FALSE
                       EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, // No creation flags
                       NULL,                                                      // Use parent's environment block
                       "D:\\Library\\Desktop",                                    // Use parent's starting directory
                       &si.StartupInfo,                                           // Pointer to STARTUPINFO structure
                       &pi)                                                       // Pointer to PROCESS_INFORMATION structure
    ) {
        printLastError();
        exit(1);
    }

    if (!CreateProcess(NULL,                                                      // No module name (use command line)
                       const_cast<char *>(commandLine.c_str()),                   // Command line
                       NULL,                                                      // Process handle not inheritable
                       NULL,                                                      // Thread handle not inheritable
                       TRUE,                                                      // Set handle inheritance to FALSE
                       EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, // No creation flags
                       NULL,                                                      // Use parent's environment block
                       "D:\\Library\\Desktop",                                    // Use parent's starting directory
                       &si2.StartupInfo,                                          // Pointer to STARTUPINFO structure
                       &pi2)                                                      // Pointer to PROCESS_INFORMATION structure
    ) {
        printLastError();
        exit(1);
    }

    HANDLE hWait1 = INVALID_HANDLE_VALUE;

    RegisterWaitForSingleObject(
        &hWait1, pi.hProcess, onWaitForSingleObject, NULL, INFINITE, WT_EXECUTEONLYONCE);

    if (hWait1 == NULL) {
        printLastError();
        exit(1);
    }

    HANDLE hWait2 = INVALID_HANDLE_VALUE;

    RegisterWaitForSingleObject(
        &hWait2, pi2.hProcess, onWaitForSingleObject2, NULL, INFINITE, WT_EXECUTEONLYONCE);

    if (hWait2 == NULL) {
        printLastError();
        exit(1);
    }

    std::thread readThread(readProcessOutputThread);
    int cnt = 0;
    while (!isProcess1Exited && !isProcess2Exited) {
        ++cnt;
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }

    BOOL ret = UnregisterWait(hWait1);
    hWait1 = INVALID_HANDLE_VALUE;

    if (ret == FALSE) {
        printLastError();
    }

    ret = UnregisterWait(hWait2);
    hWait2 = INVALID_HANDLE_VALUE;

    if (ret == FALSE) {
        printLastError();
    }

    CloseHandle(pi.hThread);   // Close the thread handle
    CloseHandle(pi.hProcess);  // Close the process handle
    CloseHandle(pi2.hThread);  // Close the thread handle
    CloseHandle(pi2.hProcess); // Close the process handle
    ClosePseudoConsole(hpc);
    ClosePseudoConsole(hpc2);
    std::cout << "hpc close..." << std::endl;
    hpc = INVALID_HANDLE_VALUE;
    hpc2 = INVALID_HANDLE_VALUE;
    CloseHandle(output.write);
    CloseHandle(output2.write);
    output.write = INVALID_HANDLE_VALUE;
    output2.write = INVALID_HANDLE_VALUE;
    CloseHandle(input.read);
    CloseHandle(input2.read);
    input.read = INVALID_HANDLE_VALUE;
    input2.read = INVALID_HANDLE_VALUE;

    readThread.join();
    std::cout << "readFileThrad finished, Cancel Async IO..." << std::endl;
    CancelIoEx(output.read, &overlapped);
    CancelIoEx(output2.read, &overlapped2);
    CloseHandle(hIOCP);
    hIOCP = INVALID_HANDLE_VALUE;
    input.close();
    output.close();
    return 0;
}