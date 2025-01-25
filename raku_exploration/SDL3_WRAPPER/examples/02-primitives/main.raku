use NativeCall;
use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Render;
use SDL3::Stdinc;
use SDL3::Rect;
# We create a NULL constant to better represent that we PASS NULL
# and not just a number that is coincidentally ZERO
constant $NULL = 0;

constant $HEIGHT = 480;
constant $WIDTH  = 640;
    
my SDL3::Video::Window $window = SDL3::Video::Window.new;
my SDL3::Event::Event $event = SDL3::Event::Event.new;
my SDL3::Render::Renderer $renderer = SDL3::Render::Renderer.new;
my $points = CArray[SDL3::Rect::FPoint].allocate(500);

# Same as in C
#for (i = 0; i < SDL_arraysize(points); i++) {
#    points[i].x = (SDL_randf() * 440.0f) + 100.0f;
#    points[i].y = (SDL_randf() * 280.0f) + 100.0f;
#}

for ^500 -> $i {
    $points[$i].x = ((SDL3::Stdinc::Randf() * 440.0) + 100.0).Num;
    $points[$i].y = ((SDL3::Stdinc::Randf() * 280.0) + 100.0).Num;
}


SDL3::Init::InitSubSystem(SDL3::Init::INIT_FLAGS::VIDEO);

$window = SDL3::Video::CreateWindow(
    "Raku SDL3 Example", 
    $WIDTH, $HEIGHT, 
    0);

$renderer = SDL3::Render::CreateRenderer($window, $NULL);


my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;

while $running {

    while (SDL3::Event::PollEvent $event)
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
    }

    SDL3::Render::SetRenderDrawColor($renderer, 33, 33, 33, 255);
    SDL3::Render::RenderClear($renderer);

    # draw a filled rectangle in the middle of the canvas.
    # blue, full alpha
    SDL3::Render::SetRenderDrawColor($renderer, 0, 0, 255, 255);
    my SDL3::Rect::FRect $rect = SDL3::Rect::FRect.new;
    $rect.x = 100.Num;
    $rect.y = 100.Num;
    $rect.w = 440.Num;
    $rect.h = 280.Num;
    # RenderFillRect expects a rect struct as a pointer, so we need to create a rect pointer.
    my $rect-pointer = nativecast(Pointer[SDL3::Rect::FRect], $rect);
    SDL3::Render::RenderFillRect($renderer, $rect-pointer);

    # Draw some points across the canvas
    # red, full alpha
    SDL3::Render::SetRenderDrawColor($renderer, 255, 0, 0, 255);
    for ^500 -> $i {
        SDL3::Render::RenderPoint($renderer, $points[$i].x, $points[$i].y);
    }

    # yellow, full alpha
    SDL3::Render::SetRenderDrawColor($renderer, 255, 255, 0, 255);
    SDL3::Render::RenderLine($renderer, 0.Num, 0.Num, 640.Num, 480.Num);
    SDL3::Render::RenderLine($renderer, 0.Num, 480.Num, 640.Num, 0.Num);

    SDL3::Render::RenderPresent($renderer);
}
