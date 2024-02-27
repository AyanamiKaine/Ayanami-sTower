#include "command.h"

void TurnLightOnCommand::execute() {
    light_->turn_on();
}

void LightBulb::turn_on() {
    std::cout << "Light is on" << std::endl;
}

void LightBulb::turn_off() {
    std::cout << "Light is off" << std::endl;
}

void RemoteControl::set_command(std::unique_ptr<Command> command) {
    command_ = std::move(command);
}

void RemoteControl::press_button() {
    if (command_) {
        command_->execute();
    }
}
