unit module SDL3::Main;
use NativeCall;
constant $SDL-LIB = 'SDL3';

# This function is the entry point to set up callbacks.
# We want to define it so we can use sdl callbacks in Raku!
# For more see: "https://wiki.libsdl.org/SDL3/SDL_EnterAppMainCallbacks"
# ,"https://github.com/Aermoss/PySDL3/issues/4#issuecomment-2571775463"
# and "https://discourse.libsdl.org/t/solved-how-to-use-the-sdl3-callback-app-structure-in-python/56557/2"
# int SDL_EnterAppMainCallbacks(int argc, char *argv[], SDL_AppInit_func appinit, SDL_AppIterate_func appiter, SDL_AppEvent_func appevent, SDL_AppQuit_func appquit);
