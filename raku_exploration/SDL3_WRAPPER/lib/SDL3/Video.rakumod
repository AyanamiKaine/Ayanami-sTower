unit module SDL3::Video;

use NativeCall;
constant $SDL-LIB = 'SDL3';

enum Window-Flags (
    FULLSCREEN           => 0x0000000000000001,
    OPENGL               => 0x0000000000000002,
    OCCLUDED             => 0x0000000000000004,
    HIDDEN               => 0x0000000000000008,
    BORDERLESS           => 0x0000000000000010,
    RESIZABLE            => 0x0000000000000020,
    MINIMIZED            => 0x0000000000000040,
    MAXIMIZED            => 0x0000000000000080,
    MOUSE_GRABBED        => 0x0000000000000100,
    INPUT_FOCUS          => 0x0000000000000200,
    MOUSE_FOCUS          => 0x0000000000000400,
    EXTERNAL             => 0x0000000000000800,
    MODAL                => 0x0000000000001000,
    HIGH_PIXEL_DENSITY   => 0x0000000000002000,
    MOUSE_CAPTURE        => 0x0000000000004000,
    MOUSE_RELATIVE_MODE  => 0x0000000000008000,
    ALWAYS_ON_TOP        => 0x0000000000010000,
    UTILITY              => 0x0000000000020000,
    TOOLTIP              => 0x0000000000040000,
    POPUP_MENU           => 0x0000000000080000,
    KEYBOARD_GRABBED     => 0x0000000000100000,
    VULKAN               => 0x0000000010000000,
    METAL                => 0x0000000020000000,
    TRANSPARENT          => 0x0000000040000000,
    NOT_FOCUSABLE        => 0x0000000080000000,
);

# Window related functions
our class Window is repr('CPointer') {};
our sub CreateWindow(Str, int32, int32, uint64) returns Window is native($SDL-LIB) is symbol('SDL_CreateWindow') { * }
our sub DestroyWindow(Window) is native($SDL-LIB) is symbol('SDL_DestroyWindow') { * }
