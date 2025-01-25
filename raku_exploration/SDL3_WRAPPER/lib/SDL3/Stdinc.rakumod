unit module SDL3::Stdinc;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our sub Sin(num64) returns num64 is native($SDL-LIB) is symbol('SDL_sin') { * };