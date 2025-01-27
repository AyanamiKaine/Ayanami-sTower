use SDL3::Video;
use SDL3::Init;
use SDL3::Render;
unit class Renderer;

has $!window-ptr;
has $!renderer-ptr;

submethod TWEAK(:$!window-ptr) {
    $!renderer-ptr = SDL_CreateRenderer($!window-ptr, 0);
}