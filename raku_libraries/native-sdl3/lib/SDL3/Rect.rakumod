# For more see: "https://wiki.libsdl.org/SDL3/CategoryRect"
unit module SDL3::Rect;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# The structure that defines a point (using floating point values).
# For more see: "https://wiki.libsdl.org/SDL3/SDL_FPoint"
our class SDL_FPoint is repr('CStruct') is export {
    has num32 $.x is rw;
    has num32 $.y is rw;
}

# A rectangle, with the origin at the upper left (using floating point values).
# For more see: "https://wiki.libsdl.org/SDL3/SDL_FRect"
our class SDL_FRect is repr('CStruct') is export {
    has num32 $.x is rw;
    has num32 $.y is rw;    
    has num32 $.w is rw;
    has num32 $.h is rw;
}