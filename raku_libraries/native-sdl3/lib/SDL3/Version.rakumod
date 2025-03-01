# Fore more see: "https://wiki.libsdl.org/SDL3/CategoryVersion"
unit module SDL3::Version;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub SDL_GetVersion() returns int64 is native($SDL-LIB, v0) is symbol('SDL_GetVersion') is export { * }
our sub SDL_GetRevision() returns str is native($SDL-LIB, v0) is symbol('SDL_GetRevision') is export { * }