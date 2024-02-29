#include "dip.h"
#include <iostream>

void FileLogger::LogInfo(const std::string& message) {
    std::cout << message << std::endl;
};

void FileLogger::LogError(const std::string& message) {
    std::cerr << message << std::endl;
};

void AnotherFileLogger::LogInfo(const std::string& message) {
    std::cout << "Doing some extra work" << "\n";
    std::cout << message << std::endl;
};

void AnotherFileLogger::LogError(const std::string& message) {
    std::cout << "Doing some extra work" << "\n";
    std::cerr << message << std::endl;
};
