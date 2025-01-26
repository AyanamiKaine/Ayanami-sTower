unit module SDL3::Render;
use SDL3::Rect; 
# You can refrence other modules from the same, the language server simply shits it self...

use NativeCall;
constant $SDL-LIB = 'SDL3';

our class Renderer is repr('CPointer') is export {};
our class FRectPointer is repr('CPointer') is export {};

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateWindowAndRenderer"
our sub CreateWindowAndRenderer(Str, uint32, uint32, uint32, uint32) returns Bool is native($SDL-LIB) is symbol('SDL_CreateWindowAndRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateRenderer"
our sub CreateRenderer(Pointer, uint32) returns Renderer is native($SDL-LIB) is symbol('SDL_CreateRenderer') is export { * }

our sub SetRenderDrawColor(Renderer, uint8, uint8, uint8, uint8) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColor') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetRenderDrawColorFloat"
our sub SetRenderDrawColorFloat(Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColorFloat') is export { * };

our sub RenderPresent(Renderer) is native($SDL-LIB) is symbol('SDL_RenderPresent') is export { * }

our sub RenderClear(Renderer) is native($SDL-LIB) is symbol('SDL_RenderClear') is export { * }

our sub DestroyRender(Renderer) is native($SDL-LIB) is symbol('SDL_DestroyRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderRect"
our sub RenderRect(Renderer, FRectPointer) returns Bool is native($SDL-LIB) is symbol('SDL_RenderRect') is export { * };
# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderFillRect"
our sub RenderFillRect(Renderer, FRectPointer) returns Bool is native($SDL-LIB) is symbol('SDL_RenderFillRect') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoints"
our sub RenderPoints(Renderer, CArray[FPoint], int32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoints') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoint"
our sub RenderPoint(Renderer, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoint') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderLine"
our sub RenderLine(Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderLine') is export { * }