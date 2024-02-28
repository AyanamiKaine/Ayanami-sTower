#include "state.h"
#include <memory>

int main (int argc, char *argv[]) {
    
    TextEditor editor;

    // The default state is all upper case;
    editor.type("this should be all uppper case");

    editor.setState(std::make_unique<LowercaseState>());
    editor.type("This Should Be All Lower Case");
    
    return 0;
}
