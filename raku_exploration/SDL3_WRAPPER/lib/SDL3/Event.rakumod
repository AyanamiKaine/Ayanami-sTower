unit module SDL3::Event;

use NativeCall;
constant $SDL-LIB = 'SDL3';

class CommonEvent is repr('CUnion') {
    has uint32 $.type;
    has uint32 $.timestamp;
}

class DisplayEvent is repr('CUnion') {
    has uint32 $.type;
    has uint32 $.timestamp;
    has uint32 $.display;
    has int8   $.event;
    has int32  $.data1;
}

# For more see: "https://wiki.libsdl.org/SDL3/SDL_WindowEvent"
class WindowEvent is repr('CUnion') {
    has uint32 $.type;
    has uint64 $.timestamp;
    has uint32 $.windowID;
    has int8   $.event;
    has int32  $.data1;
    has int32  $.data2;
}

class QuitEvent is repr('CUnion') {

    has uint32 $.type;
    has uint32 $.reserved;
    has uint64 $.timestamp; # In nanoseconds, populated using SDL_GetTicksNS()
} 

# See for more: "https://wiki.libsdl.org/SDL3/SDL_Event"
class Event is repr('CUnion') {
    has uint32        $.type;
    has CommonEvent   $.common;
    has DisplayEvent  $.display;
    has WindowEvent   $.window;
    has QuitEvent     $.quit;
    has CArray[uint8] $.padding = CArray[uint8].new(0 xx 128);
}


# For more See: "https://wiki.libsdl.org/SDL3/SDL_PollEvent"
our sub PollEvent(Event) returns bool is native($SDL-LIB) is symbol('SDL_PollEvent') { * }