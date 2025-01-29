use lib 'lib'; # Relative path to the 'lib' directory
use NativeCall;

use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
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
    "Raku SDL3 Example", 
    $WIDTH, $HEIGHT, 
    SDL3::Video::SDL_WindowFlags::RESIZABLE);

$renderer = SDL3::Render::SDL_CreateRenderer($window, Str);

my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;


my $my-render-function = sub () {
                SDL3::Render::SDL_RenderClear($renderer);
                # We need to turn the rat value into a uint64.
                # By default divisions are turned into rat values 1/2
                my uint64 $now = (SDL3::Timer::SDL_GetTicks() / 1000.0).uint64;  # convert from milliseconds to seconds.
                #choose the color for the frame we will draw. The sine wave trick makes it fade between colors smoothly. */

                # Why are we saying $red.Num? because the above expressions produce rat values
                # As an example 1/2, we need to manually turn it into a float using the Num() 
                # function.
                my num32 $red = (0.5 + 0.5 * sin($now)).Num;
                my num32 $green = (0.5 + 0.5 * sin($now + π * 2 / 3)).Num;
                my num32 $blue = (0.5 + 0.5 * sin($now + π * 4 / 3)).Num;

                SDL3::Render::SDL_SetRenderDrawColorFloat(
                    $renderer, 
                    $red, $green, $blue, 
                    $SDL-ALPHA-OPAQUE-FLOAT
                );

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

