use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Render;
use SDL3::Stdinc;
use SDL3::Log;
# We create a NULL constant to better represent that we PASS NULL
# and not just a number that is coincidentally ZERO
constant $NULL = 0;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;
 
my SDL3::Video::SDL_Window $window = SDL3::Video::SDL_Window.new;

my SDL3::Render::SDL_Renderer $renderer = SDL3::Render::SDL_Renderer.new;
my SDL3::Event::SDL_Event $event = SDL3::Event::SDL_Event.new;

SDL3::Init::SDL_InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::SDL_CreateWindow(
    "Raku SDL3 Example", 
    $WIDTH, $HEIGHT, 
    SDL3::Video::SDL_WindowFlags::RESIZABLE);

$renderer = SDL3::Render::SDL_CreateRenderer($window, $NULL);


my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;

while $running {
    if (SDL3::Event::SDL_WaitEvent $event)
    {
        # To correctly print we must flush manually
        # because sdl seems to otherwise prevent it
        # say $event.type;
        # $*OUT.flush;
        # Probably because SDL_LOG, does in someway capture it
        # The right way to print to the console in SDL would be
        # using its logging function.
        
        given $event.type {
            when SDL3::Event::Event-Type::QUIT 
            { 
                $running = False;
            }
        }

        SDL_Log($event.type.Str)
    }

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
}

# For some reason when we try to quit and destroy
# We get heap corruption, I wonder why?
#SDL3::Render::DestroyRender($renderer);
#SDL3::Video::DestroyWindow($window);
#SDL3::Init::Quit();

