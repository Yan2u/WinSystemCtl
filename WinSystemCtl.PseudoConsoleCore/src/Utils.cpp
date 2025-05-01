#include "Utils.hpp"

#include <random>
#include <stdexcept>

bool Utils::_useStdRuntimeError = true;
bool Utils::_useErrorCallback = false;
Utils::ErrorCallback Utils::_errorCallback = nullptr;

void Utils::UseStdRuntimeError() {
    _useStdRuntimeError = true;
    _useErrorCallback = false;
}

void Utils::UseErrorCallback(ErrorCallback callback) {
    _useStdRuntimeError = false;
    _useErrorCallback = true;
    _errorCallback = callback;
}

std::string Utils::GetRandomFileName() {
    constexpr char kBase32Chars[] = "abcdefghijklmnopqrstuvwxyz012345";
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<> dis(0, 31);
    auto randChar = [&] { return kBase32Chars[dis(gen)]; };
    return std::format(
        "\\\\.\\pipe\\mypipe_{}{}{}{}{}{}{}{}.{}{}{}",
        randChar(), randChar(), randChar(), randChar(),
        randChar(), randChar(), randChar(), randChar(),
        randChar(), randChar(), randChar());
}

std::string Utils::GetLastWin32ErrorMessage() {
    DWORD errCode = GetLastError();
    LPVOID lpMsgBuf;
    FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                   NULL, errCode, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&lpMsgBuf, 0, NULL);
    std::string msg((LPSTR)lpMsgBuf);
    LocalFree(lpMsgBuf);
    return msg;
}

void Utils::ThrowLastWin32Error(int line, const char *file) {
    DWORD errCode = GetLastError();
    if (_useErrorCallback && _errorCallback != nullptr) {
        _errorCallback(errCode, line, file);
    } else {
        std::string msg = std::format("File {}, Line {}, ErrorCode: {}, ErrorMessage: {}", file, line, errCode, GetLastWin32ErrorMessage());
        throw std::runtime_error(msg);
    }
}

std::string Utils::GetFailedHResultMessage(HRESULT hr) {
    LPVOID lpMsgBuf;
    FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                   NULL, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&lpMsgBuf, 0, NULL);
    std::string msg((LPSTR)lpMsgBuf);
    LocalFree(lpMsgBuf);
    return msg;
}

void Utils::SafeCloseHandle(HANDLE &handle) {
    if (handle != INVALID_HANDLE_VALUE && handle != NULL) {
        if (CloseHandle(handle) == FALSE) {
            throw std::runtime_error("Failed to close handle: " + GetLastWin32ErrorMessage());
        }
        handle = INVALID_HANDLE_VALUE;
    }
}