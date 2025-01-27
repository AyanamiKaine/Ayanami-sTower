use Anima::Window;
use Anima::Renderer;

use SDL3::Video;
use SDL3::Event;

unit class App;

has Window $!window;
has Renderer $!renderer;
has Bool $.IsRunning is rw = True;
has SDL_Event $.event;
submethod TWEAK(:$name = "Example", :$heigth, :$width) {
        $!window = Window.new(name => $name, heigth => $heigth, width => $width);
        $!renderer = Renderer.new(window-ptr => $!window.window-ptr);
}

method handle-event(&handle-event-callback)
{
    while (SDL_PollEvent $!event)
    {
        &handle-event-callback($!event)
    }
}

method quit()
{
    $.IsRunning = False;
}