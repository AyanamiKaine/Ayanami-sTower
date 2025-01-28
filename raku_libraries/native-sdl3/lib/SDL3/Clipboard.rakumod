unit module SDL3::Clipboard;
use NativeCall;
constant $SDL-LIB = 'SDL3';

# IMPORTANT TO USE THIS YOU MUST INITIALIZE THE VIDEO SUBSYSTEM!

# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetClipboardText";
our sub SDL_SetClipboardText(Str is encoded('utf8')) returns bool is native($SDL-LIB, v0) is symbol('SDL_SetClipboardText') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetClipboardText";
our sub SDL_GetClipboardText() returns Str is encoded('utf8') is native($SDL-LIB, v0) is symbol('SDL_GetClipboardText') is export { * }