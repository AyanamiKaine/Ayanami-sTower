use lib 'lib'; # Relative path to the 'lib' directory
use NativeCall;

use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Rect;
use SDL3::Render;
use SDL3::Stdinc;
use SDL3::Log;
use SDL3::Error;
# We create a NULL constant to better represent that we PASS NULL
# and not just a number that is coincidentally ZERO
constant $NULL = 0;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;
 
my SDL3::Video::SDL_Window $window = SDL3::Video::SDL_Window.new or SDL3::Error::SDL_GetError();

my SDL3::Render::SDL_Renderer $renderer = SDL3::Render::SDL_Renderer.new;
my SDL3::Event::SDL_Event $event = SDL3::Event::SDL_Event.new;

SDL3::Init::SDL_InitSubSystem(SDL3::Init::SDL_INIT_EVERYTHING);

$window = SDL3::Video::SDL_CreateWindow(
    "Raku SDL3 RenderGeometry Example", 
    $WIDTH, $HEIGHT, 
    SDL3::Video::SDL_WindowFlags::RESIZABLE);

$renderer = SDL3::Render::SDL_CreateRenderer($window, Str);

my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;


my $vert = SDL_Vertex.new();
$vert.position.x = 2e0;


my $my-render-function = sub () {
                SDL3::Render::SDL_RenderClear($renderer);





                SDL3::Render::SDL_RenderClear($renderer);
                SDL3::Render::SDL_RenderPresent($renderer);
                return False;
};

# THIS IS WHERE THE MAGIC HAPPENS
# What we want to do it to redraw the WINDOW_EXPOSED event happens
SDL_SetEventFilter(
    sub ($_, $event) {
        given $event.type {
            when WINDOW_EXPOSED
            { 
                $my-render-function();
                return False;
            }
        }
        

        return True;
        
        }, Str);

while $running {

    while (SDL_PollEvent $event)
    {
        given $event.type {
            when Event-Type::QUIT 
            { 
                $running = False;
            }
        }
    }
    $my-render-function();
}

