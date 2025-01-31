# For more see: "https://wiki.libsdl.org/SDL3/CategoryPower"
unit module SDL3::Power;
use NativeCall;
constant $SDL-LIB = 'SDL3';

# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetPowerInfo"
our sub SDL_GetPowerInfo(Pointer[int64] $seconds, Pointer[int64] $percent) is native($SDL-LIB, v0) returns int64 is export { * }  
