use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;


my SDL3::Video::SDL_Window $window = SDL3::Video::SDL_Window.new;

SDL3::Init::SDL_InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::SDL_CreateWindow("Raku SDL3 Example", $WIDTH, $HEIGHT, 0);

SDL3::Timer::SDL_Delay($DELAY);

SDL3::Video::SDL_DestroyWindow($window);
SDL3::Init::SDL_Quit();