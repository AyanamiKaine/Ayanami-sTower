#include "strategy.h"

void LeftFormatter::format(const std::string &text){
    std::cout << std::left << text << std::endl;
}

void CenterFormatter::format(const std::string &text) {
    int width = 80; 
    int padding = (width - text.length()) / 2;

    // Apply padding on both sides
    std::cout << std::setw(padding) << ' '   // Left padding
              << text 
              << std::setw(padding) << ' '   // Right padding
              << std::endl; 
}

void RightFormatter::format(const std::string &text){
    std::cout << std::right << text;
}

void TextEditor::setFormatter(std::unique_ptr<TextFormatter> formatter) {
    this->formatter = std::move(formatter); // Use move semantics
}

void TextEditor::publishText(const std::string &text) {
    if (formatter) {
        formatter->format(text);
    }
}