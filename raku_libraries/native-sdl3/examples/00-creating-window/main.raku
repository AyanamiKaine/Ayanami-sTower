use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;

constant $HEIGHT = 600;
constant $WIDTH  = 800;
constant $DELAY  = 5000;


my SDL3::Video::Window $window = SDL3::Video::Window.new;

SDL3::Init::InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::CreateWindow("Raku SDL3 Example", $WIDTH, $HEIGHT, 0);

SDL3::Timer::Delay($DELAY);

SDL3::Video::DestroyWindow($window);
SDL3::Init::Quit();