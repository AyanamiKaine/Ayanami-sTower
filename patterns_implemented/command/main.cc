#include "command.h"

int main() {
    auto light = std::make_shared<LightBulb>();
    auto light_on_command = std::make_unique<TurnLightOnCommand>(light);

    RemoteControl control;
    control.set_command(std::move(light_on_command));
    control.press_button(); // Output: Light is on

    return 0;
}