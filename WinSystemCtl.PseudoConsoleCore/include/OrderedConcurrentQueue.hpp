#pragma once

#include <queue>
#include <mutex>

template <typename T>
class OrderedConcurrentQueue {
  private:
    std::queue<T> _queue;
    std::mutex _mutex;

  public:
    void Push(const T &item);
    bool PushNonblocking(const T &item);

    bool TryPop(T &item);
    bool TryPopNonblocking(T &item);

    T &Front();
    bool FrontNonblocking(T &item);

    void PopNoreturn();
};

//
// Impl
//

template <typename T>
void OrderedConcurrentQueue<T>::Push(const T &item) {
    std::lock_guard<std::mutex> lock(_mutex);
    _queue.push(item);
}

template <typename T>
bool OrderedConcurrentQueue<T>::PushNonblocking(const T &item) {
    std::unique_lock<std::mutex> lock(_mutex, std::defer_lock);
    if (lock.try_lock()) {
        _queue.push(item);
        return true;
    }
    return false;
}

template <typename T>
bool OrderedConcurrentQueue<T>::TryPop(T &item) {
    std::lock_guard<std::mutex> lock(_mutex);
    if (_queue.empty()) {
        return false;
    }
    item = _queue.front();
    _queue.pop();
    return true;
}

template <typename T>
bool OrderedConcurrentQueue<T>::TryPopNonblocking(T &item) {
    std::unique_lock<std::mutex> lock(_mutex, std::defer_lock);
    if (lock.try_lock()) {
        if (_queue.empty()) {
            return false;
        }
        item = _queue.front();
        _queue.pop();
        return true;
    }
    return false;
}

template <typename T>
T &OrderedConcurrentQueue<T>::Front() {
    std::lock_guard<std::mutex> lock(_mutex);
    return _queue.front();
}

template <typename T>
bool OrderedConcurrentQueue<T>::FrontNonblocking(T &item) {
    std::unique_lock<std::mutex> lock(_mutex, std::defer_lock);
    if (lock.try_lock()) {
        if (_queue.empty()) {
            return false;
        }
        item = _queue.front();
        return true;
    }
    return false;
}

template <typename T>
void OrderedConcurrentQueue<T>::PopNoreturn() {
    std::lock_guard<std::mutex> lock(_mutex);
    if (!_queue.empty()) {
        _queue.pop();
    }
}