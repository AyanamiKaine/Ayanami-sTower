use Anima::Window;
use Anima::Renderer;

use SDL3::Video;
use SDL3::Event;
use SDL3::Log;
unit class App;

has Window $!window;
has Renderer $!renderer;
has Bool $.IsRunning is rw = True;
has SDL_Event $.event is rw = SDL_Event.new();

submethod TWEAK(:$name = "Example", :$heigth, :$width) {
        $!window = Window.new(name => $name, heigth => $heigth, width => $width);
        $!renderer = Renderer.new(window-ptr => $!window.window-ptr);
}

method handle-event(&handle-event-callback)
{
    while (SDL_WaitEvent $!event and $.IsRunning)
    {
        #&handle-event-callback($!event);
        given $!event.type {
            when Event-Type::QUIT 
            { 
                self.quit();
            }
        }
    }
}

method quit()
{
    $.IsRunning = False;
}