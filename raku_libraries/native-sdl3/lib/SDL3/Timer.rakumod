# For more see: "https://wiki.libsdl.org/SDL3/CategoryTimer"
unit module SDL3::Timer;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# This function waits a specified number of milliseconds before returning. It waits at least the specified time, but possibly longer due to OS scheduling
our sub SDL_Delay(uint32 $delay-time) is native($SDL-LIB, v0) is symbol('SDL_Delay') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetTicks"
our sub SDL_GetTicks() returns uint64 is native($SDL-LIB, v0) is symbol('SDL_GetTicks') is export { * }
