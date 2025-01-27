use lib 'lib'; # Relative path to the 'lib' directory
use Anima::Window;
use Anima::App;

my $app = App.new(name=> "Raku App", heigth => 600, width => 460);

while $app.IsRunning {
    $app.handle-event(-> $event {});
}