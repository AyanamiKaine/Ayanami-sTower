use SDL3::Video;
use SDL3::Init;

unit class Window;

has Str $.name;
has $.heigth;
has $.width;
has SDL3::Video::Window $.window-ptr is rw;

submethod TWEAK(:$!name = "Example", :$!heigth, :$!width) {
    say "Initializing window with height: ", $!heigth, " and width: ", $!width;

    SDL3::Init::InitSubSystem(SDL3::Init::INIT_EVERYTHING);

    $!window-ptr = SDL3::Video::CreateWindow($!name, $!width, $!heigth, SDL3::Video::Window-Flags::RESIZABLE);
}

submethod DESTROY {
    SDL3::Video::DestroyWindow($!window-ptr);
}
