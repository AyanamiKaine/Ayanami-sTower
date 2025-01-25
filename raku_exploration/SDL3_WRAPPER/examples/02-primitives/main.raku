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
    
my Window $window = Window.new;
my Event $event = Event.new;
my Renderer $renderer = Renderer.new;
my $points = CArray[FPoint].allocate(500);

# Same as in C
#for (i = 0; i < SDL_arraysize(points); i++) {
#    points[i].x = (SDL_randf() * 440.0f) + 100.0f;
#    points[i].y = (SDL_randf() * 280.0f) + 100.0f;
#}

for ^500 -> $i {
    $points[$i].x = ((Randf() * 440.0) + 100.0).Num;
    $points[$i].y = ((Randf() * 280.0) + 100.0).Num;
}


InitSubSystem(INIT_FLAGS::VIDEO);

$window = CreateWindow(
    "Raku SDL3 Example Renderer Primitives", 
    $WIDTH, $HEIGHT, 
    0);

$renderer = CreateRenderer($window, $NULL);


my $running = True;
my num32 $SDL-ALPHA-OPAQUE-FLOAT = 1.0.Num;

while $running {

    while (SDL3::Event::PollEvent $event)
    {
        given $event.type {
            when SDL3::Event::Event-Type::QUIT 
            { 
                $running = False;
            }
        }
    }

    SetRenderDrawColor($renderer, 33, 33, 33, 255);
    RenderClear($renderer);

    # draw a filled rectangle in the middle of the canvas.
    # blue, full alpha
    SetRenderDrawColor($renderer, 0, 0, 255, 255);
    my FRect $rect = FRect.new;
    $rect.x = 100.Num;
    $rect.y = 100.Num;
    $rect.w = 440.Num;
    $rect.h = 280.Num;
    # RenderFillRect expects a rect struct as a pointer, so we need to create a rect pointer.
    my $rect-pointer = nativecast(Pointer[FRect], $rect);
    RenderFillRect($renderer, $rect-pointer);

    # Draw some points across the canvas
    # red, full alpha
    SetRenderDrawColor($renderer, 255, 0, 0, 255);
    for ^500 -> $i {
        RenderPoint($renderer, $points[$i].x, $points[$i].y);
    }

    SetRenderDrawColor($renderer, 0, 255, 0, 255);
    $rect.x += 30.Num;
    $rect.y += 30.Num;
    $rect.w -= 60.Num;
    $rect.h -= 60.Num;
    RenderRect($renderer, $rect-pointer);

    # yellow, full alpha
    SetRenderDrawColor($renderer, 255, 255, 0, 255);
    RenderLine($renderer, 0.Num, 0.Num, 640.Num, 480.Num);
    RenderLine($renderer, 0.Num, 480.Num, 640.Num, 0.Num);

    RenderPresent($renderer);
}
