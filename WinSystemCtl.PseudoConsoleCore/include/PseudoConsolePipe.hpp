#pragma once
#include <stdexcept>
#include <string>
#include <windows.h>

class PseudoConsolePipe {
  private:
    std::string _pipeName;

  public:
    HANDLE hRead;
    HANDLE hWrite;
    void Close();
    PseudoConsolePipe(int bufferSize);

    ~PseudoConsolePipe();
};