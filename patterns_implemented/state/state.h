#include <memory>
#include <string>

// State Base Class
class TextEditorState {
public:
    virtual ~TextEditorState() {}
    virtual void handleInput(std::string input) = 0;
};

// Concrete States
class UppercaseState : public TextEditorState {
public:
    void handleInput(std::string input) override;
};

class LowercaseState : public TextEditorState {
public:
    void handleInput(std::string input) override;
};

class TextEditor {
private:
    std::unique_ptr<TextEditorState> state;
public:
    TextEditor() : state(std::make_unique<UppercaseState>()) {}

    void setState(std::unique_ptr<TextEditorState> newState);
    void type(std::string input);
};
