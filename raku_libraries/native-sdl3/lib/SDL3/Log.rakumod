unit module SDL3::Log;
use NativeCall;
constant $SDL-LIB = 'SDL3';
our sub SDL_Log(Str) is native($SDL-LIB) is export { * }
