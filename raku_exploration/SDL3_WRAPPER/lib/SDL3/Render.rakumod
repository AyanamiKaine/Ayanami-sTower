unit module SDL3::Render;

use NativeCall;
constant $SDL-LIB = 'SDL3';

our class Renderer is repr('CPointer') {};

class FPoint is repr('CStruct') {
    has num32 $.x is rw;
    has num32 $.y is rw;
}
# Define the FRect type, for the render module, I have no idea how i can refrence the FRect class defined in the Rect Module
#class FRect is repr('CStruct') {
#	has num32 $.x;
#	has num32 $.y;
#	has num32 $.w;
#	has num32 $.h;
#}


# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateWindowAndRenderer"
our sub CreateWindowAndRenderer(Str, uint32, uint32, uint32, uint32) returns Bool is native($SDL-LIB) is symbol('SDL_CreateWindowAndRenderer') { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateRenderer"
our sub CreateRenderer(Pointer, uint32) returns Renderer is native($SDL-LIB) is symbol('SDL_CreateRenderer'){ * }

our sub SetRenderDrawColor(Renderer, uint8, uint8, uint8, uint8) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColor') { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetRenderDrawColorFloat"
our sub SetRenderDrawColorFloat(Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColorFloat') { * };

our sub RenderPresent(Renderer) is native($SDL-LIB) is symbol('SDL_RenderPresent') { * }

our sub RenderClear(Renderer) is native($SDL-LIB) is symbol('SDL_RenderClear') { * }

our sub DestroyRender(Renderer) is native($SDL-LIB) is symbol('SDL_DestroyRenderer') { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderFillRect"
our sub RenderFillRect(Renderer, Pointer) returns Bool is native($SDL-LIB) is symbol('SDL_RenderFillRect') { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoints"
our sub RenderPoints(Renderer, CArray[FPoint], int32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoints') { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoint"
our sub RenderPoint(Renderer, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoint') { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderLine"
our sub RenderLines(Renderer, num32, num32, num32, num32) returns Bool is symbol('SDL_RenderLine'){ * }