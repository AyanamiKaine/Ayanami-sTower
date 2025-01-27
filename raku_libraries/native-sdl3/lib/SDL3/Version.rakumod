unit module SDL3::Version;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub SDL_GetVersion() returns int64 is native($SDL-LIB) is symbol('SDL_GetVersion') is export { * }
