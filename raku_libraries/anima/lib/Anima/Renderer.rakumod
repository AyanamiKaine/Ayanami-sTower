use SDL3::Video;
use SDL3::Init;
use SDL3::Render;
unit class Renderer;

has $!window-ptr;
has $!renderer-ptr;

submethod TWEAK(:$!window-ptr) {
    $!renderer-ptr = SDL_CreateRenderer($!window-ptr, 0);
}

method Clear()
{
    SDL_RenderClear($!renderer-ptr);
}

method Present()
{
    SDL_RenderPresent($!renderer-ptr);
}