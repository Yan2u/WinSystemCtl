#pragma once
#include <fstream>
#include <string>
#include <format>
#include <filesystem>

#define LOG_INFO(format, ...) Logger::Instance().LogInfoDetail(__FILE__, __LINE__, format, __VA_ARGS__)
#define LOG_ERROR(format, ...) Logger::Instance().LogErrorDetail(__FILE__, __LINE__, format, __VA_ARGS__)
#define LOG_WARN(format, ...) Logger::Instance().LogWarningDetail(__FILE__, __LINE__, format, __VA_ARGS__)

class Logger {
  public:
    static constexpr const char *LOG_FILE_FOLDER = "\\logs";
    static constexpr const char *LOG_FILE_TEMPLATE = "{}\\latest.log";
    static constexpr const char *STORE_LOG_FILE_TEMPLATE = "{}\\{}.log";
    static constexpr uint64_t MAX_LOG_FILE_SIZE = 1024 * 1024 * 10; // 10MB
    static std::string GetTimeStampFileName();
    static std::string GetTimeStamp();

  private:
    std::string _currentLogFile;
    std::filesystem::path _logFolder;
    std::ofstream _logStream;

    bool _useStdout = false;
    bool _useFile = false;

    void initializeLogFile();

  public:
    Logger();
    ~Logger();
    static Logger &Instance();

    void UseStdout();
    void UseFile();

    void Log(const std::string &message);

    template <typename... Args>
    void LogInfo(const char *format, Args &&...args) {
        Log(std::format("INFO | {} | ", GetTimeStamp()) + std::vformat(format, std::make_format_args(args...)));
    }

    template <typename... Args>
    void LogError(const char *format, Args &&...args) {
        Log(std::format("ERROR | {} | ", GetTimeStamp()) + std::vformat(format, std::make_format_args(args...)));
    }

    template <typename... Args>
    void LogWarning(const char *format, Args &&...args) {
        Log(std::format("WARN | {} | ", GetTimeStamp()) + std::vformat(format, std::make_format_args(args...)));
    }

    template <typename... Args>
    void LogInfoDetail(const char *file, int line, const char *format, Args &&...args) {
        Log(std::format("INFO | {} | {}:{} | ", GetTimeStamp(), file, line) + std::vformat(format, std::make_format_args(args...)));
    }

    template <typename... Args>
    void LogErrorDetail(const char *file, int line, const char *format, Args &&...args) {
        Log(std::format("ERROR | {} | {}:{} | ", GetTimeStamp(), file, line) + std::vformat(format, std::make_format_args(args...)));
    }

    template <typename... Args>
    void LogWarningDetail(const char *file, int line, const char *format, Args &&...args) {
        Log(std::format("WARN | {} | {}:{} | ", GetTimeStamp(), file, line) + std::vformat(format, std::make_format_args(args...)));
    }

    void Close();
};