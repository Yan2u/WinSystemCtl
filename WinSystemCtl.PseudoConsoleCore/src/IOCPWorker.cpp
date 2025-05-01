#include "IOCPWorker.hpp"
#include "Utils.hpp"
#include "Logger.hpp"

#include <mutex>
#include <stdexcept>
#include <thread>
#include <winerror.h>
#include <wingdi.h>

IOCPWorker::IOCPWorker() {
    _hIOCP = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
    if (_hIOCP == NULL) {
        THROW_WIN32_ERROR();
    }
    _completionKey = 0;
    _queryQueueTask = std::thread(&IOCPWorker::queryQueue, this);
}

void IOCPWorker::queryQueue() {
    BOOL ret = FALSE;
    DWORD bytesTransferred = 0;
    uint64_t key;
    LPOVERLAPPED lpOverlapped = NULL;
    while (!_stopQueryQueueTask) {
        while (true) {
            if (_stopQueryQueueTask) { break; }
            {
                std::lock_guard<std::mutex> lock(_mutex);
                ret = GetQueuedCompletionStatus(_hIOCP, &bytesTransferred, &key, &lpOverlapped, 0);
            }
            if (ret == FALSE) {
                if (GetLastError() == WAIT_TIMEOUT) {
                    std::this_thread::sleep_for(std::chrono::milliseconds(_sleepDuration));
                    _sleepDuration = min(_sleepDuration + 100, MAX_SLEEP_DURATION);
                } else {
                    break;
                }
            } else {
                _sleepDuration = 0;
                break;
            }
        }
        if (_stopQueryQueueTask) {
            break;
        }
        if (ret == FALSE) {
            DWORD code = GetLastError();
            if (code == ERROR_BROKEN_PIPE || code == ERROR_OPERATION_ABORTED) {
                if (_handlers.count(key) != 0) {
                    auto handler = _handlers.at(key);
                    if (handler != nullptr) {
                        handler(0, code);
                    }
                }
                continue;
            } else {
                THROW_WIN32_ERROR();
            }
        }
        if (bytesTransferred == 0) {
            continue;
        }
        if (_handlers.count(key) != 0) {
            auto handler = _handlers.at(key);
            if (handler != nullptr) {
                handler(bytesTransferred, ERROR_SUCCESS);
            }
        }
    }
}

int IOCPWorker::Bind(HANDLE handle, IOCPEventHandler callback) {
    std::lock_guard<std::mutex> lock(_mutex);
    _hIOCP = CreateIoCompletionPort(handle, _hIOCP, _completionKey, 0);
    if (_hIOCP == INVALID_HANDLE_VALUE) {
        THROW_WIN32_ERROR();
    }
    int key = _completionKey;
    _handlers.insert({key, callback});
    ++_completionKey;
    return key;
}

void IOCPWorker::Close() {
    _stopQueryQueueTask = true;
    if (_queryQueueTask.joinable()) {
        _queryQueueTask.join();
    }
    Utils::SafeCloseHandle(_hIOCP);
}

IOCPWorker::~IOCPWorker() {
    Close();
}
