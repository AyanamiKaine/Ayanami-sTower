unit module SDL3::Log;
use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub GetError() returns Str is native($SDL-LIB) is symbol('SDL_GetError') is export { * }
