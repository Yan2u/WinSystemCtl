#pragma once
#include <string>
#include <windows.h>
#include <format>
#include <iostream>

#define THROW_WIN32_ERROR()                                                                                                                                            \
    std::cout << std::format("File {}, Line {}, ErrorCode: {}, ErrorMessage: {}", __FILE__, __LINE__, GetLastError(), Utils::GetLastWin32ErrorMessage()) << std::endl; \
    std::cout.flush();                                                                                                                                                 \
    Utils::ThrowLastWin32Error(__LINE__, __FILE__);

class Utils {
  public:
    typedef void (*ErrorCallback)(DWORD, int, const char *);

  private:
    static bool _useStdRuntimeError;
    static bool _useErrorCallback;
    static ErrorCallback _errorCallback;

  public:
    static void UseStdRuntimeError();
    static void UseErrorCallback(ErrorCallback callback);
    static std::string GetRandomFileName();
    static std::string GetLastWin32ErrorMessage();
    static std::string GetFailedHResultMessage(HRESULT hr);
    static void SafeCloseHandle(HANDLE &handle);
    static void ThrowLastWin32Error(int line, const char *file);
};