unit module SDL3::Pixels;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# For more see: "https://wiki.libsdl.org/SDL3/SDL_Color"
our class SDL_Color is repr('CStruct') is export {
    has uint8 $.r;
    has uint8 $.g;
    has uint8 $.b;
    has uint8 $.a;   
}

our class SDL_FColor is repr('CStruct') is export {
    has num32 $.r;
    has num32 $.g;
    has num32 $.b;
    has num32 $.a;   
}