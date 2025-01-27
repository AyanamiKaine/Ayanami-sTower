unit module SDL3::SLog;
use NativeCall;
constant $SDL-LIB = 'SDL3';
# Should the string be freed by me? or does SDL do it?, if sdl does it this is wrong. As the GC will free the string.
our sub SDL_Log(Str) is native($SDL-LIB) is export { * }
