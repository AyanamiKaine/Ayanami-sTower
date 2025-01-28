unit module SDL3::Pixels;

use NativeCall;
constant $SDL-LIB = 'SDL3';

# For more see: "https://wiki.libsdl.org/SDL3/SDL_Color"
our class SDL_Color is repr('CStruct') is export {
    has uint8 $.r is rw;
    has uint8 $.g is rw;
    has uint8 $.b is rw;
    has uint8 $.a is rw;   
}
# For more see: "https://wiki.libsdl.org/SDL3/SDL_FColor"
our class SDL_FColor is repr('CStruct') is export {
    has num32 $.r is rw;
    has num32 $.g is rw;
    has num32 $.b is rw;
    has num32 $.a is rw;   
}