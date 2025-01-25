unit module SDL3::Rect;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# The structure that defines a point (using floating point values).
# For more see: "https://wiki.libsdl.org/SDL3/SDL_FPoint"
our class FPoint is repr('CStruct') {
    has num32 $.x;
    has num32 $.y;
}

# A rectangle, with the origin at the upper left (using floating point values).
# For more see: "https://wiki.libsdl.org/SDL3/SDL_FRect"
our class FRect is repr('CStruct') {
    has num32 $.x;
    has num32 $.y;    
    has num32 $.w;
    has num32 $.h;
}