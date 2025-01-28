unit module SDL3::SLog;
use NativeCall;
constant $SDL-LIB = 'SDL3';
# For more see: "https://wiki.libsdl.org/SDL3/SDL_Log"
# Should the string be freed by me? or does SDL do it?, if sdl does it this is wrong. As the GC will free the string.
our sub SDL_Log(Str) is native($SDL-LIB, v0) is export { * }
