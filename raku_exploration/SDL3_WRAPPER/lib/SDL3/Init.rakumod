unit module SDL3::Init;

use NativeCall;
constant $SDL-LIB = 'SDL3';

enum INIT_FLAGS (
    AUDIO    => 0x00000010,
    VIDEO    => 0x00000020,
    JOYSTICK => 0x00000200,
    HAPTIC   => 0x00001000,
    GAMEPAD  => 0x00002000,
    EVENTS   => 0x00004000,
    SENSOR   => 0x00008000,
    CAMERA   => 0x00010000,
);

enum AppResult (
    APP_CONTINUE    => 0,
    APP_SUCCESS     => 1,
    APP_FAILURE     => 2,
);

constant INIT_EVERYTHING = [+|] INIT_FLAGS::.values;

# This function and SDL_Init() are interchangeable.
# (bool) Returns true on success or false on failure; call SDL_GetError() for more information.
our sub InitSubSystem(uint32) returns bool is native($SDL-LIB) is symbol('SDL_InitSubSystem') { * }

# See for more: https://wiki.libsdl.org/SDL3/SDL_Quit
our sub Quit() is native($SDL-LIB) is symbol('SDL_Quit') { * }
