unit module SDL3::Stdinc;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub Sin(num64) returns num64 is native($SDL-LIB) is symbol('SDL_sin') { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_randf"
our sub Randf() returns num32 is native($SDL-LIB) is symbol('SDL_randf') { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_arraysize"
our sub SDL_arraysize(CArray:D $array --> int) {
    # Return how many elements the CArray holds
    return $array.elems;
}