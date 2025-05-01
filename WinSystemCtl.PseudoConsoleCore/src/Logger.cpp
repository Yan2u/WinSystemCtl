#include "Logger.hpp"

#include <ctype.h>
#include <filesystem>
#include <string>
#include <iostream>
#include <windows.h>

Logger &Logger::Instance() {
    static Logger instance;
    return instance;
}

std::string Logger::GetTimeStampFileName() {
    SYSTEMTIME st;
    GetLocalTime(&st);
    return std::format("{:04}_{:02}_{:02}_{:02}_{:02}_{:02}_{:03}", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
}

std::string Logger::GetTimeStamp() {
    SYSTEMTIME st;
    GetLocalTime(&st);
    return std::format("{:04}-{:02}-{:02} {:02}:{:02}:{:02}.{:03}", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
}

void Logger::initializeLogFile() {
    // check log folder
    _logFolder = std::filesystem::current_path();
    _logFolder += LOG_FILE_FOLDER;

    if (!std::filesystem::exists(_logFolder)) {
        if (!std::filesystem::create_directory(_logFolder)) {
            throw std::runtime_error("Failed to create log folder: " + _logFolder.string());
        }
    }

    // check log file
    _currentLogFile = std::format(LOG_FILE_TEMPLATE, _logFolder.string());
    _logStream.open(_currentLogFile, std::ios::app | std::ios::out);
    if (!_logStream.is_open()) {
        throw std::runtime_error("Failed to open log file: " + _currentLogFile);
    }
}

void Logger::UseFile() {
    _useFile = true;
    _useStdout = false;
    initializeLogFile();
}

void Logger::UseStdout() {
    _useStdout = true;
    _useFile = false;
}

void Logger::Log(const std::string &message) {
    if (_useFile) {
        if (_logStream.is_open()) {
            _logStream << message << std::endl;
            _logStream.flush();

            // check size
            _logStream.seekp(0, std::ios::end);
            if (_logStream.tellp() > MAX_LOG_FILE_SIZE) {
                _logStream.close();

                // move lastest log file to archive
                std::string _storeLogFile = std::format(STORE_LOG_FILE_TEMPLATE, _logFolder.string(), GetTimeStamp());
                std::filesystem::rename(_currentLogFile, _storeLogFile);

                // create new log file
                _logStream.open(_currentLogFile, std::ios::out | std::ios::app);
                if (!_logStream.is_open()) {
                    throw std::runtime_error("Failed to open log file: " + _currentLogFile);
                }
            }

        } else {
            throw std::runtime_error("Log stream is not open.");
        }
    }

    if (_useStdout) {
        std::cout << message << std::endl;
        std::cout.flush();
    }
}

void Logger::Close() {
    if (_logStream.is_open()) {
        _logStream.close();
    }
}

Logger::Logger() {
}

Logger::~Logger() {
    Close();
}