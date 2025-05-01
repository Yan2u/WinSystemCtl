#include "PseudoConsolePipe.hpp"
#include "Utils.hpp"

#include <format>
#include <handleapi.h>
#include <random>

PseudoConsolePipe::PseudoConsolePipe(int bufferSize) {
    _pipeName = Utils::GetRandomFileName();
    hRead = CreateNamedPipe(_pipeName.c_str(),
                            PIPE_ACCESS_INBOUND | FILE_FLAG_OVERLAPPED,
                            PIPE_TYPE_BYTE | PIPE_WAIT,
                            1, bufferSize, bufferSize, 0, NULL);
    if (hRead == INVALID_HANDLE_VALUE) {
        THROW_WIN32_ERROR();
    }

    hWrite = CreateFile(_pipeName.c_str(),
                        GENERIC_WRITE,
                        0,
                        NULL,
                        OPEN_EXISTING,
                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                        NULL);
    if (hWrite == INVALID_HANDLE_VALUE) {
        CloseHandle(hRead);
        THROW_WIN32_ERROR();
    }
}

void PseudoConsolePipe::Close() {
    Utils::SafeCloseHandle(hRead);
    Utils::SafeCloseHandle(hWrite);
}

PseudoConsolePipe::~PseudoConsolePipe() {
    Close();
}