unit module SDL3::Stdinc;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub SDL_sin(num64) returns num64 is native($SDL-LIB, v0) is symbol('SDL_sin') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_randf"
our sub SDL_randf() returns num32 is native($SDL-LIB, v0) is symbol('SDL_randf') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_arraysize"
our sub SDL_arraysize(CArray:D $array --> int) is export {
    # Return how many elements the CArray holds
    return $array.elems;
}