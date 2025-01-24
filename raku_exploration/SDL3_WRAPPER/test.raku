use NativeCall;

constant $SDL-LIB = 'lib/SDL3';


constant $SDL_INIT_VIDEO = 0x00000020;
constant $SDL_PI_D = 3.14159265358979323846264338327950288;
constant $SDL_ALPHA_OPAQUE_FLOAT = 1.0;
# Event type
constant $SDL_EVENT_QUIT = 0x200;
# App return result codes
enum SDL_AppResult (
    'SDL_APP_SUCCESS'   => 0,
    'SDL_APP_FAILURE'   => 1,
    'SDL_APP_CONTINUE'  => 2,
);

# SDL types
class SDL_Window is repr('CPointer') {}
class SDL_Renderer is repr('CPointer') {}
class SDL_Event is repr('CStruct') {
    has uint32 $.type;
    # Add other event fields as needed for handling different events
}
# Main function types
sub SDL_AppInit(Pointer, int32, Pointer) returns int32  is native($SDL-LIB) is symbol('SDL_AppInit'){ * };
sub SDL_AppEvent(Pointer, SDL_Event is rw) returns int32  is native($SDL-LIB) is symbol('SDL_AppEvent'){ * };
sub SDL_AppIterate(Pointer) returns int32  is native($SDL-LIB) is symbol('SDL_AppIterate'){ * };
sub SDL_AppQuit(Pointer, int32 ) is native($SDL-LIB) is symbol('SDL_AppQuit'){ * };

# SDL functions
sub SDL_Init(uint32) returns int32 is native($SDL-LIB) { * }
sub SDL_SetAppMetadata(Str, Str, Str) returns int32 is native($SDL-LIB) is symbol('SDL_SetAppMetadata'){ * }
sub SDL_CreateWindowAndRenderer(Str, int32, int32, uint32, SDL_Window is rw, SDL_Renderer is rw) returns int32 is native($SDL-LIB) { * }
sub SDL_GetError() returns Str is native($SDL-LIB) { * }
sub SDL_GetTicks() returns uint64 is native($SDL-LIB) { * }
sub SDL_SetRenderDrawColorFloat(SDL_Renderer, num32, num32, num32, num32) returns int32 is native($SDL-LIB) { * }
sub SDL_RenderClear(SDL_Renderer) returns int32 is native($SDL-LIB) { * }
sub SDL_RenderPresent(SDL_Renderer) is native($SDL-LIB) { * }

# Global variables (similar to your C code)
my SDL_Window $window = SDL_Window.new;
my SDL_Renderer $renderer = SDL_Renderer.new;

sub app-init(Pointer $appstate, int32 $argc, Pointer $argv) returns int32 {
    SDL_SetAppMetadata("Example Renderer Clear", "1.0", "com.example.renderer-clear");

    if SDL_Init($SDL_INIT_VIDEO) != 0 {
        return SDL_APP_FAILURE;
    }
}

if SDL_CreateWindowAndRenderer("examples/renderer/clear", 640, 480, 0, $window, $renderer) != 0 {
    
    loop
    {
        
    }
    
    return SDL_APP_FAILURE;
}