use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Render;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;


my SDL3::Video::Window $window = SDL3::Video::Window.new;
my SDL3::Event::Event $event = SDL3::Event::Event.new;
my SDL3::Render::Renderer $renderer = SDL3::Render::Renderer.new;

SDL3::Init::InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::CreateWindow("Raku SDL3 Example", $WIDTH, $HEIGHT, 0x0000000000000020);

class NULL is repr('CPointer') { }
# If we pass NULL to the createRendere SDL choices what driver to use
# For now i dont know how I can easily pass a NULLPTR, without
# creating it myself like the above code.
$renderer = SDL3::Render::CreateRenderer($window, 0);

my $frame-counter = 0;

while True {

    while (SDL3::Event::PollEvent $event)
    {
        say $event.type;
    }
    SDL3::Render::SetRenderDrawColor($renderer, 0, 0, 0, 255);
    SDL3::Render::RenderClear($renderer);
    SDL3::Render::RenderPresent($renderer);
}

SDL3::Timer::Delay($DELAY);

SDL3::Render::DestroyRender($renderer);
SDL3::Video::DestroyWindow($window);

SDL3::Init::Quit();