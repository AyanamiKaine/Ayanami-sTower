use SDL3::Video;
use SDL3::Init;

unit class Window;

has Str $.name;
has $.heigth;
has $.width;
has SDL_Window $.window-ptr is rw;

submethod TWEAK(:$!name = "Example", :$!heigth, :$!width) {
    say "Initializing window with height: ", $!heigth, " and width: ", $!width;

    SDL_InitSubSystem(SDL_INIT_EVERYTHING);

    $!window-ptr = SDL_CreateWindow($!name, $!width, $!heigth, SDL_WindowFlags::RESIZABLE);
}

submethod DESTROY {
   SDL_DestroyWindow($!window-ptr);
}
