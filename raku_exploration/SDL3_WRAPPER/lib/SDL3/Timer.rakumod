unit module SDL3::Timer;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# This function waits a specified number of milliseconds before returning. It waits at least the specified time, but possibly longer due to OS scheduling
our sub Delay(uint32) is native($SDL-LIB) is symbol('SDL_Delay') { * }
