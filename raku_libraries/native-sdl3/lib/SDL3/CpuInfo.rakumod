unit module SDL3::CpuInfo;
use NativeCall;
constant $SDL-LIB = 'SDL3';

# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetNumLogicalCPUCores";
our sub SDL_GetNumLogicalCPUCores() returns int64 is native($SDL-LIB, v0) is symbol('SDL_GetNumLogicalCPUCores') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetCPUCacheLineSize";
our sub SDL_GetCPUCacheLineSize() returns int64 is native($SDL-LIB, v0) is symbol('SDL_GetCPUCacheLineSize') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasAltiVec";
our sub SDL_HasAltiVec() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasAltiVec') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasMMX";
our sub SDL_HasMMX() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasMMX') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasSSE";
our sub SDL_HasSSE() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasSSE') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasSSE2";
our sub SDL_HasSSE2() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasSSE2') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasSSE3";
our sub SDL_HasSSE3() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasSSE3') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasSSE41";
our sub SDL_HasSSE41() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasSSE41') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasSSE42";
our sub SDL_HasSSE42() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasSSE42') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasAVX";
our sub SDL_HasAVX() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasAVX') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasAVX2";
our sub SDL_HasAVX2() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasAVX2') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasAVX512F";
our sub SDL_HasAVX512F() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasAVX512F') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasARMSIMD";
our sub SDL_HasARMSIMD() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasARMSIMD') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasNEON";
our sub SDL_HasNEON() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasNEON') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_HasLSX";
our sub SDL_HasLSX() returns bool is native($SDL-LIB, v0) is symbol('SDL_HasLSX') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetSystemRAM";
our sub SDL_GetSystemRAM() returns int64 is native($SDL-LIB, v0) is symbol('SDL_GetSystemRAM') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetSIMDAlignment";
our sub SDL_GetSIMDAlignment() returns size_t is native($SDL-LIB, v0) is symbol('SDL_GetSIMDAlignment') is export { * }