unit module SDL3::Log;
use NativeCall;
constant $SDL-LIB = 'SDL3';
our sub Log(Str) is native($SDL-LIB) is symbol('SDL_Log') is export { * }
