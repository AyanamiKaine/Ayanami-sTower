unit module SDL3::Event;
use NativeCall;
use SDL3::Video;


constant $SDL-LIB = 'SDL3';

our enum Event-Type is export (
    FIRST                    => 0,      # Unused (do not remove)
    QUIT                     => 0x100,  # User-requested quit
    TERMINATING              => 0x101,  # The application is being terminated by the OS
    LOW_MEMORY               => 0x102,  # The application is low on memory
    WILL_ENTER_BACKGROUND    => 0x103,  # The application is about to enter the background
    DID_ENTER_BACKGROUND     => 0x104,  # The application did enter the background
    WILL_ENTER_FOREGROUND    => 0x105,  # The application is about to enter the foreground
    DID_ENTER_FOREGROUND     => 0x106,  # The application is now interactive
    LOCALE_CHANGED           => 0x107,  # The user's locale preferences have changed
    SYSTEM_THEME_CHANGED     => 0x108,  # The system theme changed

    DISPLAY_ORIENTATION       => 0x151,  # Display orientation has changed to data1
    DISPLAY_ADDED             => 0x152,  # Display has been added to the system
    DISPLAY_REMOVED           => 0x153,  # Display has been removed from the system
    DISPLAY_MOVED             => 0x154,  # Display has changed position
    DISPLAY_DESKTOP_MODE_CHANGED => 0x155, # Display has changed desktop mode
    DISPLAY_CURRENT_MODE_CHANGED => 0x156, # Display has changed current mode
    DISPLAY_CONTENT_SCALE_CHANGED => 0x157, # Display has changed content scale
    DISPLAY_FIRST            => 0x151,
    DISPLAY_LAST             => 0x157,

    WINDOW_SHOWN             => 0x202,  # Window has been shown
    WINDOW_HIDDEN            => 0x203,  # Window has been hidden
    WINDOW_EXPOSED           => 0x204,  # Window has been exposed and should be redrawn and can be redrawn directly from event watchers for this event 
    WINDOW_MOVED             => 0x205,  # Window has been moved to data1, data2
    WINDOW_RESIZED           => 0x206,  # Window has been resized to data1xdata2
    WINDOW_PIXEL_SIZE_CHANGED => 0x207, # The pixel size of the window has changed to data1xdata2
    WINDOW_METAL_VIEW_RESIZED => 0x208, # The pixel size of a Metal view associated with the window has changed
    WINDOW_MINIMIZED         => 0x209,  # Window has been minimized
    WINDOW_MAXIMIZED         => 0x20A,  # Window has been maximized
    WINDOW_RESTORED          => 0x20B,  # Window has been restored to normal size and position
    WINDOW_MOUSE_ENTER       => 0x20C,  # Window has gained mouse focus
    WINDOW_MOUSE_LEAVE       => 0x20D,  # Window has lost mouse focus
    WINDOW_FOCUS_GAINED      => 0x20E,  # Window has gained keyboard focus
    WINDOW_FOCUS_LOST        => 0x20F,  # Window has lost keyboard focus
    WINDOW_CLOSE_REQUESTED   => 0x210,  # The window manager requests that the window be closed
    WINDOW_HIT_TEST          => 0x211,  # Window had a hit test that wasn't SDL_HITTEST_NORMAL
    WINDOW_ICCPROF_CHANGED   => 0x212,  # The ICC profile of the window's display has changed
    WINDOW_DISPLAY_CHANGED   => 0x213,  # Window has been moved to display data1
    WINDOW_DISPLAY_SCALE_CHANGED => 0x214, # Window display scale has been changed
    WINDOW_SAFE_AREA_CHANGED => 0x215, # The window safe area has been changed
    WINDOW_OCCLUDED          => 0x216,  # The window has been occluded
    WINDOW_ENTER_FULLSCREEN   => 0x217,  # The window has entered fullscreen mode
    WINDOW_LEAVE_FULLSCREEN   => 0x218,  # The window has left fullscreen mode
    WINDOW_DESTROYED         => 0x219,  # The window with the associated ID is being or has been destroyed
    WINDOW_HDR_STATE_CHANGED => 0x21A, # Window HDR properties have changed
    WINDOW_FIRST             => 0x202,
    WINDOW_LAST              => 0x21A,

    KEY_DOWN                 => 0x300,  # Key pressed
    KEY_UP                   => 0x301,  # Key released
    TEXT_EDITING             => 0x302,  # Keyboard text editing (composition)
    TEXT_INPUT               => 0x303,  # Keyboard text input
    KEYMAP_CHANGED           => 0x304,  # Keymap changed
    KEYBOARD_ADDED           => 0x305,  # A new keyboard has been inserted into the system
    KEYBOARD_REMOVED         => 0x306,  # A keyboard has been removed
    TEXT_EDITING_CANDIDATES  => 0x307,  # Keyboard text editing candidates

    MOUSE_MOTION             => 0x400,  # Mouse moved
    MOUSE_BUTTON_DOWN        => 0x401,  # Mouse button pressed
    MOUSE_BUTTON_UP          => 0x402,  # Mouse button released
    MOUSE_WHEEL              => 0x403,  # Mouse wheel motion
    MOUSE_ADDED              => 0x404,  # A new mouse has been inserted into the system
    MOUSE_REMOVED            => 0x405,  # A mouse has been removed

    JOYSTICK_AXIS_MOTION     => 0x600,  # Joystick axis motion
    JOYSTICK_BALL_MOTION     => 0x601,  # Joystick trackball motion
    JOYSTICK_HAT_MOTION      => 0x602,  # Joystick hat position change
    JOYSTICK_BUTTON_DOWN     => 0x603,  # Joystick button pressed
    JOYSTICK_BUTTON_UP       => 0x604,  # Joystick button released
    JOYSTICK_ADDED           => 0x605,  # A new joystick has been inserted into the system
    JOYSTICK_REMOVED         => 0x606,  # An opened joystick has been removed
    JOYSTICK_BATTERY_UPDATED => 0x607,  # Joystick battery level change
    JOYSTICK_UPDATE_COMPLETE => 0x608,  # Joystick update is complete

    GAMEPAD_AXIS_MOTION      => 0x650,  # Gamepad axis motion
    GAMEPAD_BUTTON_DOWN      => 0x651,  # Gamepad button pressed
    GAMEPAD_BUTTON_UP        => 0x652,  # Gamepad button released
    GAMEPAD_ADDED            => 0x653,  # A new gamepad has been inserted into the system
    GAMEPAD_REMOVED          => 0x654,  # A gamepad has been removed
    GAMEPAD_REMAPPED         => 0x655,  # The gamepad mapping was updated
    GAMEPAD_TOUCHPAD_DOWN    => 0x656,  # Gamepad touchpad was touched
    GAMEPAD_TOUCHPAD_MOTION  => 0x657,  # Gamepad touchpad finger was moved
    GAMEPAD_TOUCHPAD_UP      => 0x658,  # Gamepad touchpad finger was lifted
    GAMEPAD_SENSOR_UPDATE    => 0x659,  # Gamepad sensor was updated
    GAMEPAD_UPDATE_COMPLETE  => 0x65A,  # Gamepad update is complete
    GAMEPAD_STEAM_HANDLE_UPDATED => 0x65B, # Gamepad Steam handle has changed

    FINGER_DOWN              => 0x700,
    FINGER_UP                => 0x701,
    FINGER_MOTION            => 0x702,
    FINGER_CANCELED          => 0x703,

    CLIPBOARD_UPDATE         => 0x900,  # The clipboard or primary selection changed

    DROP_FILE                => 0x1000, # The system requests a file open
    DROP_TEXT                => 0x1001, # text/plain drag-and-drop event
    DROP_BEGIN               => 0x1002, # A new set of drops is beginning (NULL filename)
    DROP_COMPLETE            => 0x1003, # Current set of drops is now complete (NULL filename)
    DROP_POSITION            => 0x1004, # Position while moving over the window

    AUDIO_DEVICE_ADDED       => 0x1100, # A new audio device is available
    AUDIO_DEVICE_REMOVED     => 0x1101, # An audio device has been removed.
    AUDIO_DEVICE_FORMAT_CHANGED => 0x1102, # An audio device's format has been changed by the system.

    SENSOR_UPDATE            => 0x1200, # A sensor was updated

    PEN_PROXIMITY_IN         => 0x1300, # Pressure-sensitive pen has become available
    PEN_PROXIMITY_OUT        => 0x1301, # Pressure-sensitive pen has become unavailable
    PEN_DOWN                 => 0x1302, # Pressure-sensitive pen touched drawing surface
    PEN_UP                   => 0x1303, # Pressure-sensitive pen stopped touching drawing surface
    PEN_BUTTON_DOWN          => 0x1304, # Pressure-sensitive pen button pressed
    PEN_BUTTON_UP            => 0x1305, # Pressure-sensitive pen button released
    PEN_MOTION               => 0x1306, # Pressure-sensitive pen is moving on the tablet
    PEN_AXIS                 => 0x1307, # Pressure-sensitive pen angle/pressure/etc changed

    CAMERA_DEVICE_ADDED      => 0x1400, # A new camera device is available
    CAMERA_DEVICE_REMOVED    => 0x1401, # A camera device has been removed.
    CAMERA_DEVICE_APPROVED   => 0x1402, # A camera device has been approved for use by the user.
    CAMERA_DEVICE_DENIED     => 0x1403, # A camera device has been denied for use by the user.

    RENDER_TARGETS_RESET     => 0x2000, # The render targets have been reset and their contents need to be updated
    RENDER_DEVICE_RESET      => 0x2001, # The device has been reset and all textures need to be recreated
    RENDER_DEVICE_LOST       => 0x2002, # The device has been lost and can't be recovered.

    PRIVATE0                 => 0x4000,
    PRIVATE1                 => 0x4001,
    PRIVATE2                 => 0x4002,
    PRIVATE3                 => 0x4003,

    POLL_SENTINEL            => 0x7F00, # Signals the end of an event poll cycle
    USER                     => 0x8000, # Events USER through LAST are for your use
    LAST                     => 0xFFFF, # This last event is only for bounding internal arrays
    ENUM_PADDING             => 0x7FFFFFFF # This just makes sure the enum is the size of Uint32
);

our class SDL_CommonEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.timestamp;
}

our class SDL_DisplayEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.timestamp;
    has uint32 $.display;
    has int8   $.event;
    has int32  $.data1;
}

# For more see: "https://wiki.libsdl.org/SDL3/SDL_WindowEvent"
our class SDL_WindowEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint64 $.timestamp;
    has uint32 $.windowID;
    has int8   $.event;
    has int32  $.data1;
    has int32  $.data2;
}

our class SDL_QuitEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.reserved;
    has uint64 $.timestamp; # In nanoseconds, populated using SDL_GetTicksNS()
} 
our class SDL_KeyboardDeviceEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.reserved;
    has uint64 $.timestamp; # In nanoseconds, populated using SDL_GetTicksNS()
    has uint32 $.which;     # The keyboard instance id
} 
our class SDL_KeyboardEvent is repr('CStruct') is export is rw {
    has uint32  $.type;
    has uint32  $.reserved;
    has uint64  $.timestamp;       # In nanoseconds, populated using SDL_GetTicksNS()
    has uint32  $.windowID;        # The window with keyboard focus, if any 
    has uint32  $.which;           # The keyboard instance id, or 0 if unknown or virtual */
    has uint32  $.scancode;        # SDL physical key code */
    has uint32  $.key;             # SDL virtual key code */
    has uint32  $.mod;             # current key modifiers */
    has uint16  $.raw;             # The platform dependent scancode for this event */
    has bool    $.down;            # true if the key is pressed */
    has bool    $.repeat;          # true if this is a key repeat */
}

our class SDL_TextEditingEvent is repr('CStruct') is export is rw {
    has uint32  $.type;
    has uint32  $.reserved;
    has uint64  $.timestamp;       # In nanoseconds, populated using SDL_GetTicksNS()
    has uint32  $.windowID;        # The window with keyboard focus, if any 
    has Str     $.text;            # The editing text
    has int32   $.start;           # The start cursor of selected editing text, or -1 if not set
    has int32   $.length;          # The length of selected editing text, or -1 if not set
}

our class SDL_TextEditingCandidatesEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.reserved;
    has uint64 $.timestamp;
    has uint32 $.windowID;
    has Pointer[Str] $.candidates;  # Pointer to an array of strings (const char * const *)
    has int32 $.num_candidates;     # The number of strings in `candidates
    has int32 $.selected_candidate; # The index of the selected candidate, or -1 if no candidate is selected
    has bool $.horizontal;          # true if the list is horizontal, false if it's vertical
    has uint8 $.padding1;
    has uint8 $.padding2;
    has uint8 $.padding3;
}

our class SDL_TextInputEvent is repr('CStruct') is export is rw {
    has uint32 $.type;
    has uint32 $.reserved;
    has uint64 $.timestamp;
    has uint32 $.windowID;
    has Str  $.text;
}

our class SDL_MouseDeviceEvent is repr('CStruct') is export is rw {
    has uint32 $.type;          #/**< SDL_EVENT_MOUSE_ADDED or SDL_EVENT_MOUSE_REMOVED *
    has uint32 $.reserved;
    has uint64 $.timestamp;     #/**< In nanoseconds, populated using SDL_GetTicksNS() */
    has uint32 $.which;         #/**< The mouse instance id */
}


our class SDL_MouseMotionEvent is repr('CStruct') is export is rw {
    has uint32 $.type;          
    has uint32 $.reserved;
    has uint64 $.timestamp;     #/**< In nanoseconds, populated using SDL_GetTicksNS() */
    has uint32 $.windowID;      #/**< The window with mouse focus, if any */
    has uint32 $.which;         #/**< The mouse instance id */
    has uint32 $.state;         #/**< The current button state */
    has num32  $.x;             #/**< X coordinate, relative to window */
    has num32  $.y;             #/**< Y coordinate, relative to window */
    has num32  $.xrel;          #/**< The relative motion in the X direction */
    has num32  $.yrel;          #/**< The relative motion in the Y direction */
}

our class SDL_MouseButtonEvent is repr('CStruct') is export is rw {
    has uint32 $.type;          
    has uint32 $.reserved;
    has uint64 $.timestamp;     #/**< In nanoseconds, populated using SDL_GetTicksNS() */
    has uint32 $.windowID;      #/**< The window with mouse focus, if any */
    has uint32 $.which;         #/**< The mouse instance id */
    has uint8  $.button;        #/**< The mouse button index */
    has bool   $.down;          #/**< true if the button is pressed */
    has uint8  $.clicks;        #/**< 1 for single-click, 2 for double-click, etc. */
    has uint8  $.padding;
    has num32  $.x;             #/**< X coordinate, relative to window */
    has num32  $.y;             #/**< Y coordinate, relative to window */
}

our class SDL_MouseWheelEvent is repr('CStruct') is export is rw {
    has uint32 $.type;          #/**< SDL_EVENT_MOUSE_WHEEL */
    has uint32 $.reserved;
    has uint64 $.timestamp;     #/**< In nanoseconds, populated using SDL_GetTicksNS() */
    has uint32 $.windowID;      #/**< The window with mouse focus, if any */
    has uint32 $.which;         #/**< The mouse instance id */
    has num32  $.x;             #/**< The amount scrolled horizontally, positive to the right and negative to the left */
    has num32  $.y;             #/**< The amount scrolled vertically, positive away from the user and negative toward the user */
    has uint32 $.direction;
    has num32  $.mouse_x;       #/**< X coordinate, relative to window */
    has num32  $.mouse_y;       #/**< Y coordinate, relative to window */
}

our class SDL_JoyDeviceEvent is repr('CStruct') is export is rw {
    has uint32 $.type;          #/**< SDL_EVENT_JOYSTICK_ADDED or SDL_EVENT_JOYSTICK_REMOVED or SDL_EVENT_JOYSTICK_UPDATE_COMPLETE */
    has uint32 $.reserved;
    has uint64 $.timestamp;     #/**< In nanoseconds, populated using SDL_GetTicksNS() */
    has uint32 $.which;         # /**< The joystick instance id */
}

# "CStructs and CUnions can be in turn referenced by—or embedded into—a surrounding CStruct and CUnion. To say the former we use has as usual, and to do the latter we use the HAS declarator instead"

# When you have a struct with pointers with a type of a struct use has, if they are value use HAS

#Raku Declarator	C Equivalent	    Relationship
#has	            Pointer (Type *)	Referenced/Indirect
#HAS	            Value (Type)	    Embedded/Direct

# I believe there is a memory corruption here, somehow memory gets written to the wrong addresses,
# for now I am unsure if the problem is here. I think the reason is two folded. I didnt implement all
# Events and my knowledge about CUnions is lacking. 
# For more see: "https://wiki.libsdl.org/SDL3/SDL_Event"
our class SDL_Event is repr('CUnion') is export {
    has uint32                   $.type;
    HAS  SDL_CommonEvent         $.common;
    HAS  SDL_DisplayEvent        $.display;
    HAS  SDL_WindowEvent         $.window;
    HAS  SDL_QuitEvent           $.quit;
    HAS  SDL_KeyboardDeviceEvent $.kdevice;
    HAS  SDL_KeyboardEvent       $.key;
    HAS  SDL_TextEditingEvent    $.edit;
    HAS  SDL_TextEditingCandidatesEvent $.edit_candidates;

    HAS SDL_TextInputEvent $.text;                #/**< Text input event data */
    HAS SDL_MouseDeviceEvent $.mdevice;           #/**< Mouse device change event data */
    HAS SDL_MouseMotionEvent $.motion;            #/**< Mouse motion event data */
    HAS SDL_MouseButtonEvent $.button;            #/**< Mouse button event data */
    HAS SDL_MouseWheelEvent $.wheel;              #/**< Mouse wheel event data */
    HAS SDL_JoyDeviceEvent $.jdevice;             #/**< Joystick device change event data */
    HAS SDL_JoyAxisEvent $.jaxis;                 #/**< Joystick axis event data */
    HAS SDL_JoyBallEvent $.jball;                 #/**< Joystick ball event data */
    HAS SDL_JoyHatEvent $.jhat;                   #/**< Joystick hat event data */
    HAS SDL_JoyButtonEvent $.jbutton;             #/**< Joystick button event data */
    HAS SDL_JoyBatteryEvent $.jbattery;           #/**< Joystick battery event data */
    HAS SDL_GamepadDeviceEvent $.gdevice;         #/**< Gamepad device event data */
    HAS SDL_GamepadAxisEvent $.gaxis;             #/**< Gamepad axis event data */
    HAS SDL_GamepadButtonEvent $.gbutton;         #/**< Gamepad button event data */
    HAS SDL_GamepadTouchpadEvent $.gtouchpad;     #/**< Gamepad touchpad event data */
    HAS SDL_GamepadSensorEvent $.gsensor;         #/**< Gamepad sensor event data */
    HAS SDL_AudioDeviceEvent $.adevice;           #/**< Audio device event data */
    HAS SDL_CameraDeviceEvent $.cdevice;          #/**< Camera device event data */
    HAS SDL_SensorEvent $.sensor;                 #/**< Sensor event data */
    HAS SDL_QuitEvent $.quit;                     #/**< Quit request event data */
    HAS SDL_UserEvent $.user;                     #/**< Custom event data */
    HAS SDL_TouchFingerEvent $.tfinger;           #/**< Touch finger event data */
    HAS SDL_PenProximityEvent $.pproximity;       #/**< Pen proximity event data */
    HAS SDL_PenTouchEvent $.ptouch;               #/**< Pen tip touching event data */
    HAS SDL_PenMotionEvent $.pmotion;             #/**< Pen motion event data */
    HAS SDL_PenButtonEvent $.pbutton;             #/**< Pen button event data */
    HAS SDL_PenAxisEvent $.paxis;                 #/**< Pen axis event data */
    HAS SDL_RenderEvent $.render;                 #/**< Render event data */
    HAS SDL_DropEvent $.drop;                     #/**< Drag and drop event data */
    HAS SDL_ClipboardEvent $.clipboard;           #/**< Clipboard event data */


    has CArray[uint8] $.padding = CArray[uint8].new(0 xx 128);
}




# For more see: "https://wiki.libsdl.org/SDL3/SDL_PollEvent"
our sub SDL_PollEvent(SDL_Event is rw) returns bool is native($SDL-LIB, v0) is symbol('SDL_PollEvent') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_WaitEvent"
our sub SDL_WaitEvent(SDL_Event is rw) returns bool is native($SDL-LIB, v0) is symbol('SDL_WaitEvent') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_AddEventWatch"
our sub SDL_AddEventWatch(&callback (Pointer[void] $userdata, SDL_Event is rw  --> bool), Pointer[void] $data) returns bool is native($SDL-LIB, v0) is symbol('SDL_AddEventWatch') is export { * }
# Fore more see: "https://wiki.libsdl.org/SDL3/SDL_SetEventFilter"
our sub SDL_SetEventFilter(&callback (Pointer[void] $userdata, SDL_Event is rw  --> bool), Pointer[void] $data) returns bool is native($SDL-LIB, v0) is symbol('SDL_SetEventFilter') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_WaitEventTimeout"
our sub SDL_WaitEventTimeout(SDL_Event is rw, int32) is native($SDL-LIB, v0) is symbol('SDL_WaitEventTimeout') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_PushEvent"
our sub SDL_PushEvent(SDL_Event is rw) is native($SDL-LIB, v0) is symbol('SDL_PushEvent') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetEventEnabled"
our sub SDL_SetEventEnabled(uint32, bool) is native($SDL-LIB, v0) is symbol('SDL_SetEventEnabled') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_EventEnabled"
our sub SDL_EventEnabled(uint32) returns bool is native($SDL-LIB, v0) is symbol('SDL_EventEnabled') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_RegisterEvents"
our sub SDL_RegisterEvents(int32) returns uint32 is native($SDL-LIB, v0) is symbol('SDL_RegisterEvents') is export { * }
# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetWindowFromEvent"
our sub SDL_GetWindowFromEvent(SDL_Event is rw) returns SDL_Window  is native($SDL-LIB, v0) is symbol('SDL_GetWindowFromEvent') is export { * }
