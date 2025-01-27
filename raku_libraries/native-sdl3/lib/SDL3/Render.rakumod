unit module SDL3::Render;
use SDL3::Rect; 
# You can refrence other modules from the same, the language server simply shits it self...

use NativeCall;
constant $SDL-LIB = 'SDL3';

our class SDL_Renderer is repr('CPointer') is export {};
our class SDL_FRectPointer is repr('CPointer') is export {};

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateWindowAndRenderer"
our sub SDL_CreateWindowAndRenderer(Str, uint32, uint32, uint32, uint32) returns Bool is native($SDL-LIB) is symbol('SDL_CreateWindowAndRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateRenderer"
our sub SDL_CreateRenderer(Pointer, uint32) returns SDL_Renderer is native($SDL-LIB) is symbol('SDL_CreateRenderer') is export { * }

our sub SDL_SetRenderDrawColor(SDL_Renderer, uint8, uint8, uint8, uint8) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColor') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetRenderDrawColorFloat"
our sub SDL_SetRenderDrawColorFloat(SDL_Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_SetRenderDrawColorFloat') is export { * };

our sub SDL_RenderPresent(SDL_Renderer) is native($SDL-LIB) is symbol('SDL_RenderPresent') is export { * }

our sub SDL_RenderClear(SDL_Renderer) is native($SDL-LIB) is symbol('SDL_RenderClear') is export { * }

our sub SDL_DestroyRender(SDL_Renderer) is native($SDL-LIB) is symbol('SDL_DestroyRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderRect"
our sub SDL_RenderRect(SDL_Renderer, SDL_FRectPointer) returns Bool is native($SDL-LIB) is symbol('SDL_RenderRect') is export { * };
# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderFillRect"
our sub SDL_RenderFillRect(SDL_Renderer, SDL_FRectPointer) returns Bool is native($SDL-LIB) is symbol('SDL_RenderFillRect') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoints"
our sub SDL_RenderPoints(SDL_Renderer, CArray[SDL_FPoint], int32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoints') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoint"
our sub SDL_RenderPoint(SDL_Renderer, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderPoint') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderLine"
our sub SDL_RenderLine(SDL_Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB) is symbol('SDL_RenderLine') is export { * }