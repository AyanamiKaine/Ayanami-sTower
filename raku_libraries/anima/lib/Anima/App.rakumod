use Anima::Window;
use Anima::Renderer;

use SDL3::Video;

unit class App;

has Window $!window;
has Renderer $!renderer;

submethod TWEAK(:$name = "Example", :$heigth, :$width) {
        $!window = Window.new(name => $name, heigth => $heigth, width => $width);
        $!renderer = Renderer.new(window-ptr => $!window.window-ptr);
}