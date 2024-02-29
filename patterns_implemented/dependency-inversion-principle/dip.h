#include <memory>
#include <string>

/* We create an abstract interface that represent the interface of a logger.
 *
 * We use create dependency only on this abstract interface and not on concrete implementations of it.
 *
 * With that we can create new implementation of loggers without needing to change any used code. We could now use the old logger for anything old, and the new logger for everything new.
 *
 * Everyone that uses our logger Abstraction is open for extension (Can use another logger that is based on the abstract ILogger class) and closed for modification (The interface of the ILogger class does not change)
*/
class ILogger {
public:
    virtual void LogInfo(const std::string& message) = 0;
    virtual void LogError(const std::string& message) = 0;
};

class FileLogger : public ILogger {
public:
    void LogInfo(const std::string& message) override;
    void LogError(const std::string& message) override;
};

class AnotherFileLogger : public ILogger {
public:
    void LogInfo(const std::string& message) override;
    void LogError(const std::string& message) override;
};

class MyClass {
private:
    std::unique_ptr<ILogger> logger;

private:
    MyClass(std::unique_ptr<ILogger> logger) 
        : logger(std::move(logger)) {};

    void DoSomething();
};
