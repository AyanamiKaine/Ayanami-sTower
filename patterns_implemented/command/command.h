#include <iostream>
#include <memory>

// Abstract Command interface
class Command {
public:
    virtual ~Command() = default; // Virtual destructor for safe deallocation
    virtual void execute() = 0;
};

// Receiver
class LightBulb {
public:
    void turn_on();
    void turn_off();
};

// Concrete Command
class TurnLightOnCommand : public Command {
public:
    TurnLightOnCommand(std::shared_ptr<LightBulb> light) : light_(light) { };
    void execute() override;

private:
    std::shared_ptr<LightBulb> light_;
};

// Invoker
class RemoteControl {
public:
    void set_command(std::unique_ptr<Command> command);
    void press_button();

private:
    std::unique_ptr<Command> command_;
};