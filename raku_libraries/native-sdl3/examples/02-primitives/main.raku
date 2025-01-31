use NativeCall;
use lib 'lib'; # Relative path to the 'lib' directory
use SDL3::Video;
use SDL3::Timer;
use SDL3::Init;
use SDL3::Event;
use SDL3::Render;
use SDL3::Stdinc;
use SDL3::Rect;
use SDL3::Log;

# We create a NULL constant to better represent that we PASS NULL
# and not just a number that is coincidentally ZERO
constant $NULL = 0;

constant $HEIGHT = 480;
constant $WIDTH  = 640;
    
my SDL_Window $window =  SDL_Window.new;
my SDL_Event $event = SDL_Event.new;
my SDL_Renderer $renderer =  SDL_Renderer.new;
my $points = CArray[SDL_FPoint].allocate(500);

# Same as in C
#for (i = 0; i < SDL_arraysize(points); i++) {
#    points[i].x = (SDL_randf() * 440.0f) + 100.0f;
#    points[i].y = (SDL_randf() * 280.0f) + 100.0f;
#}

for ^500 -> $i {
    $points[$i].x = ((SDL_randf() * 440.0) + 100.0).Num;
    $points[$i].y = ((SDL_randf() * 280.0) + 100.0).Num;
}


SDL_InitSubSystem(INIT_FLAGS::VIDEO);

$window = SDL_CreateWindow(
    "Raku SDL3 Example Renderer Primitives", 
    $WIDTH, $HEIGHT, 
    SDL3::Video::SDL_WindowFlags::RESIZABLE);

$renderer = SDL3::Render::SDL_CreateRenderer($window, Nil);


my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;

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

    SDL_SetRenderDrawColor($renderer, 33, 33, 33, 255);
    SDL_RenderClear($renderer);

    # draw a filled rectangle in the middle of the canvas.
    # blue, full alpha
    SDL_SetRenderDrawColor($renderer, 0, 0, 255, 255);
    my SDL_FRect $rect =  SDL_FRect.new;
    $rect.x = 100.Num;
    $rect.y = 100.Num;
    $rect.w = 440.Num;
    $rect.h = 280.Num;
    # RenderFillRect expects a rect struct as a pointer, so we need to create a rect pointer.
    SDL_RenderFillRect($renderer, $rect);

    # Draw some points across the canvas
    # red, full alpha
    SDL_SetRenderDrawColor($renderer, 255, 0, 0, 255);
    for ^500 -> $i {
        SDL_RenderPoint($renderer, $points[$i].x, $points[$i].y);
    }

    SDL_SetRenderDrawColor($renderer, 0, 255, 0, 255);
    $rect.x += 30.Num;
    $rect.y += 30.Num;
    $rect.w -= 60.Num;
    $rect.h -= 60.Num;
    SDL_RenderRect($renderer, $rect);

    # yellow, full alpha
    SDL_SetRenderDrawColor($renderer, 255, 255, 0, 255);
    SDL_RenderLine($renderer, 0.Num, 0.Num, 640.Num, 480.Num);
    SDL_RenderLine($renderer, 0.Num, 480.Num, 640.Num, 0.Num);

    SDL_RenderPresent($renderer);
}
