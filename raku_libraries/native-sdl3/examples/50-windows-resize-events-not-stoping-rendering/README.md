On windows draging the window blocks the thread as long as the event is happening, this means when you check for events in the same thread as the rendering it will block as long as the user holds the window. Often times this is not necessary, to circumvent this we must run the event handling in another thread using callbacks.

This problem was fixed in SDL 2.3 see: "https://github.com/libsdl-org/SDL/issues/1059"

# The Basics

## use SDL_SetEventFilter

## C Example

```C
#include <SDL3/SDL.h>

// Assuming you have a function called MyRenderFunction that you want to call
// when SDL_EVENT_WINDOW_EXPOSED occurs.
extern void MyRenderFunction(void);


bool SDLCALL MyEventFilter(void* userdata, SDL_Event* event) {
    switch (event->type) {
        case SDL_EVENT_WINDOW_EXPOSED:
            MyRenderFunction();
            return false;
        default:
            return true;
    }
}

int main(int argc, char* argv[]) {
    // ... Your initialization code ...

    // Set the event filter. The userdata parameter can be a pointer to any data
    // you want to pass to your filter function. Here we're passing NULL as we
    // don't need any specific data in this example.
    SDL_SetEventFilter(MyEventFilter, NULL);

    // ... Rest of your application ...
    bool running = true; 
    SDL_Event event;

    while (running) {
        while (SDL_PollEvent(&event)) { 
            switch (event.type) {
                case SDL_EVENT_QUIT:
                    running = SDL_FALSE;
                    break;
                // Handle other events here...
            }
        }
        MyRenderFunction(); // Call your rendering function after processing events
    }
    return 0;
}
```
