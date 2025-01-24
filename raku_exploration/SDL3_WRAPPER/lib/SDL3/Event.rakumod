unit module SDL3::Event;

use NativeCall;
constant $SDL-LIB = 'SDL3';

class CommonEvent is repr('CStruct') {
    has uint32 $.type;
    has uint32 $.timestamp;
}

class DisplayEvent is repr('CStruct') {
    has uint32 $.type;
    has uint32 $.timestamp;
    has uint32 $.display;
    has int8   $.event;
    has int32  $.data1;
}

# For more see: "https://wiki.libsdl.org/SDL3/SDL_WindowEvent"
class WindowEvent is repr('CStruct') {
    has uint32 $.type;
    has uint64 $.timestamp;
    has uint32 $.windowID;
    has int8   $.event;
    has int32  $.data1;
    has int32  $.data2;
}

# See for more: "https://wiki.libsdl.org/SDL3/SDL_Event"
# I believe i must correctly implement ALL events and the complete
# CUnion type otherwise it wont work at all...
class Event is repr('CUnion') {
  has uint32        $.type;
  has CommonEvent   $.common;
  has DisplayEvent  $.display;
  has WindowEvent   $.window;
}


# For more See: "https://wiki.libsdl.org/SDL3/SDL_PollEvent"
our sub PollEvent(Event) returns bool is native($SDL-LIB) is symbol('SDL_PollEvent') { * }
