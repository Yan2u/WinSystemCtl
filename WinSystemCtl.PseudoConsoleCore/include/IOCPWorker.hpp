#pragma once
#include <mutex>
#include <thread>
#include <stdint.h>
#include <functional>
#include <windows.h>
#include <concurrent_unordered_map.h>

typedef std::function<void(DWORD, DWORD)> IOCPEventHandler;

class IOCPWorker {
  public:
    const int MAX_SLEEP_DURATION = 300;

  private:
    HANDLE _hIOCP;
    uint64_t _completionKey;
    std::thread _queryQueueTask;
    bool _stopQueryQueueTask = false;
    std::mutex _mutex;
    concurrency::concurrent_unordered_map<int, IOCPEventHandler> _handlers;
    int _sleepDuration = 100;

    void queryQueue();

  public:
    IOCPWorker();
    ~IOCPWorker();

    void Close();
    int Bind(HANDLE handle, IOCPEventHandler callback);
};