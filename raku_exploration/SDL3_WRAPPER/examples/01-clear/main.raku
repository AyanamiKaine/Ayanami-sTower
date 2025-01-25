use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Render;

# We create a NULL constant to better represent that we PASS NULL
# and not just a number that is coincidentally ZERO
constant $NULL = 0;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;


my SDL3::Video::Window $window = SDL3::Video::Window.new;
my SDL3::Event::Event $event = SDL3::Event::Event.new;
my SDL3::Render::Renderer $renderer = SDL3::Render::Renderer.new;


SDL3::Init::InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::CreateWindow(
    "Raku SDL3 Example", 
    $WIDTH, $HEIGHT, 
    SDL3::Video::Window-Flags::RESIZABLE);

$renderer = SDL3::Render::CreateRenderer($window, $NULL);


my $running = True;
while $running {

    while (SDL3::Event::PollEvent $event)
    {
        # To correctly print we must flush manually
        # because sdl seems to otherwise prevent it
        # say $event.type;
        # $*OUT.flush;
        
        given $event.type {
            when SDL3::Event::Event-Type::QUIT 
            { 
                $running = False;
            }
        }
    }

    constant $now = SDL_GetTicks() / 1000.0;  # convert from milliseconds to seconds.
    #choose the color for the frame we will draw. The sine wave trick makes it fade between colors smoothly. */
    constant $red = (float) (0.5 + 0.5 * SDL_sin(now));
    constant $green = (float) (0.5 + 0.5 * SDL_sin(now + SDL_PI_D * 2 / 3));
    constant $blue = (float) (0.5 + 0.5 * SDL_sin(now + SDL_PI_D * 4 / 3));

    SDL3::Render::SetRenderDrawColor($renderer, 0, 0, 0, 255);
    SDL3::Render::RenderClear($renderer);
    SDL3::Render::RenderPresent($renderer);
}

# For some reason when we try to quit and destroy
# We get heap corruption, I wonder why?
#SDL3::Render::DestroyRender($renderer);
#SDL3::Video::DestroyWindow($window);
#SDL3::Init::Quit();

