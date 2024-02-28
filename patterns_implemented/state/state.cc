#include "state.h"
#include <algorithm>
#include <cctype>
#include <iostream>
#include <memory>

void UppercaseState::handleInput(std::string input) {
    std::transform(input.begin(), input.end(), input.begin(), ::toupper);
    std::cout << input << std::endl;
};

void LowercaseState::handleInput(std::string input) {
    std::transform(input.begin(), input.end(), input.begin(), ::tolower);
    std::cout << input << std::endl;
}

void TextEditor::setState(std::unique_ptr<TextEditorState> newState) {
    state = std::move(newState);
}

void TextEditor::type(std::string input) {
    state->handleInput(input);
}
