use SDL3::Video;
use SDL3::Init;

unit class Renderer;

has $.window-ptr;
has $.renderer-ptr;

submethod TWEAK(:$!window-ptr) {
    $!renderer-ptr = SDL3::Render::CreateRenderer($!window-ptr, 0);
}