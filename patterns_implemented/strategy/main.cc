#include "strategy.h"
#include <memory>
int main() {
    TextEditor editor;

    // Create smart pointers
    auto left = std::make_unique<LeftFormatter>();
    auto center = std::make_unique<CenterFormatter>();

    editor.setFormatter(std::move(left));
    editor.publishText("This text will be left-aligned");

    editor.setFormatter(std::move(center));
    editor.publishText("This will be centered");
    return 0;
}