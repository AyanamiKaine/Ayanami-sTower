use Anima::Window;
use SDL3::Video;
unit class App;

has Window $!window;

submethod TWEAK(:$name = "Example", :$heigth, :$width) {
        $!window = Window.new(name => $name, heigth => $heigth, width => $width);
}