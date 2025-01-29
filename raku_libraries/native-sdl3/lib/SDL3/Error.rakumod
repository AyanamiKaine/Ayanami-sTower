unit module SDL3::Error;
use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub SDL_GetError() returns Str is native($SDL-LIB, v0) is export { * }
