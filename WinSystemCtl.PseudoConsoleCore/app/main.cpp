#include <codecvt>
#include <ctype.h>

#include "TaskManager.hpp"
#include "Logger.hpp"

#include <cstdio>
#include <iostream>
#include <format>
#include <locale>
#include <random>
#include <regex>
#include <thread>
#include <vector>
#include <winspool.h>

static constexpr int TASK_COUNT = 1;
std::wstring commandLine = L"C:\\Windows\\System32\\cmd.exe /c test2.exe";
std::wstring testEnvVars = L"X=1\0Y=2\0Z=3\0";
std::string ansiRegex = R"(\x1b(\[.*?[@-~]|\].*?(\x07|\x1b\\)))";
std::atomic_int finished{0};

std::atomic_int endCount{0};

void taskOutputCallback(int id, DWORD nBytes, BYTE *buffer) {
    std::string output(buffer, buffer + nBytes);
    std::string filteredOutput = std::regex_replace(output, std::regex(ansiRegex), "");
    std::cout << filteredOutput << std::endl;
    /*LOG_INFO("Task {} output: {}", id, filteredOutput);*/
}

void taskExitCallback(int id, DWORD exitCode) {
    // std::cout << std::format("Task {} exited with code: {}", id, exitCode) << std::endl;
    // std::cout.flush();
    // LOG_INFO("Task {} exited with code: {}", id, exitCode);
    ++finished;
}

std::string genRandomStr(size_t len) {
    static std::mt19937_64 rng(std::random_device{}());
    static std::uniform_int_distribution<int> dist(0, 25);
    std::string str(len, ' ');
    std::generate_n(str.begin(), len, []() { return 'a' + dist(rng); });
    return str;
}

class A {
  public:
    A() {
        std::cout << "A constructor" << std::endl;
    }

    ~A() {
        std::cout << "A destructor" << std::endl;
    }
};

int main() {
    std::locale::global(std::locale(""));
    //Logger::Instance().UseStdout();
    TaskManager manager(8192, {256, 256});
    std::vector<int> taskIds;
    for (int i = 0; i < TASK_COUNT; ++i) {
        taskIds.emplace_back(manager.Submit(L"",
                                            commandLine,
                                            L"",
                                            L"D:\\Library\\Desktop",
                                            std::bind(taskOutputCallback, i, std::placeholders::_1, std::placeholders::_2),
                                            std::bind(taskExitCallback, i, std::placeholders::_1)));
    }

    while (finished < TASK_COUNT) {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }

    std::cout << "All tasks finished." << std::endl;
    manager.Close();
    std::cout << "TaskManager closed." << std::endl;

    std::cout << "End sign found: " << endCount << std::endl;

    return 0;
}