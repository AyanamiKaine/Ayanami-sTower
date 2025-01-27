unit module SDL3::Log;
use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub SDL_GetError() returns Str is native($SDL-LIB) is export { * }
