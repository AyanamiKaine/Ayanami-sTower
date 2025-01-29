unit module SDL3::Render;
use SDL3::Rect; 
use SDL3::Pixels;
# You can refrence other modules from the same, the language server simply shits it self...

use NativeCall;
constant $SDL-LIB = 'SDL3';

# For more see: "https://wiki.libsdl.org/SDL3/SDL_PixelFormat"
our enum SDL_PixelFormat is export (
    SDL_PIXELFORMAT_UNKNOWN    => 0x0,
    SDL_PIXELFORMAT_INDEX1LSB  => 0x11100100,
    SDL_PIXELFORMAT_INDEX1MSB  => 0x11200100,
    SDL_PIXELFORMAT_INDEX2LSB  => 0x1c100200,
    SDL_PIXELFORMAT_INDEX2MSB  => 0x1c200200,
    SDL_PIXELFORMAT_INDEX4LSB  => 0x12100400,
    SDL_PIXELFORMAT_INDEX4MSB  => 0x12200400,
    SDL_PIXELFORMAT_INDEX8     => 0x13000801,
    SDL_PIXELFORMAT_RGB332     => 0x14110801,
    SDL_PIXELFORMAT_XRGB4444   => 0x15120c02,
    SDL_PIXELFORMAT_XBGR4444   => 0x15520c02,
    SDL_PIXELFORMAT_XRGB1555   => 0x15130f02,
    SDL_PIXELFORMAT_XBGR1555   => 0x15530f02,
    SDL_PIXELFORMAT_ARGB4444   => 0x15321002,
    SDL_PIXELFORMAT_RGBA4444   => 0x15421002,
    SDL_PIXELFORMAT_ABGR4444   => 0x15721002,
    SDL_PIXELFORMAT_BGRA4444   => 0x15821002,
    SDL_PIXELFORMAT_ARGB1555   => 0x15331002,
    SDL_PIXELFORMAT_RGBA5551   => 0x15441002,
    SDL_PIXELFORMAT_ABGR1555   => 0x15731002,
    SDL_PIXELFORMAT_BGRA5551   => 0x15841002,
    SDL_PIXELFORMAT_RGB565     => 0x15151002,
    SDL_PIXELFORMAT_BGR565     => 0x15551002,
    SDL_PIXELFORMAT_RGB24      => 0x17101803,
    SDL_PIXELFORMAT_BGR24      => 0x17401803,
    SDL_PIXELFORMAT_XRGB8888   => 0x16161804,
    SDL_PIXELFORMAT_RGBX8888   => 0x16261804,
    SDL_PIXELFORMAT_XBGR8888   => 0x16561804,
    SDL_PIXELFORMAT_BGRX8888   => 0x16661804,
    SDL_PIXELFORMAT_ARGB8888   => 0x16362004,
    SDL_PIXELFORMAT_RGBA8888   => 0x16462004,
    SDL_PIXELFORMAT_ABGR8888   => 0x16762004,
    SDL_PIXELFORMAT_BGRA8888   => 0x16862004,
    SDL_PIXELFORMAT_XRGB2101010 => 0x16172004,
    SDL_PIXELFORMAT_XBGR2101010 => 0x16572004,
    SDL_PIXELFORMAT_ARGB2101010 => 0x16372004,
    SDL_PIXELFORMAT_ABGR2101010 => 0x16772004,
    SDL_PIXELFORMAT_RGB48      => 0x18103006,
    SDL_PIXELFORMAT_BGR48      => 0x18403006,
    SDL_PIXELFORMAT_RGBA64     => 0x18204008,
    SDL_PIXELFORMAT_ARGB64     => 0x18304008,
    SDL_PIXELFORMAT_BGRA64     => 0x18504008,
    SDL_PIXELFORMAT_ABGR64     => 0x18604008,
    SDL_PIXELFORMAT_RGB48_FLOAT => 0x1a103006,
    SDL_PIXELFORMAT_BGR48_FLOAT => 0x1a403006,
    SDL_PIXELFORMAT_RGBA64_FLOAT => 0x1a204008,
    SDL_PIXELFORMAT_ARGB64_FLOAT => 0x1a304008,
    SDL_PIXELFORMAT_BGRA64_FLOAT => 0x1a504008,
    SDL_PIXELFORMAT_ABGR64_FLOAT => 0x1a604008,
    SDL_PIXELFORMAT_RGB96_FLOAT => 0x1b10600c,
    SDL_PIXELFORMAT_BGR96_FLOAT => 0x1b40600c,
    SDL_PIXELFORMAT_RGBA128_FLOAT => 0x1b208010,
    SDL_PIXELFORMAT_ARGB128_FLOAT => 0x1b308010,
    SDL_PIXELFORMAT_BGRA128_FLOAT => 0x1b508010,
    SDL_PIXELFORMAT_ABGR128_FLOAT => 0x1b608010,
    SDL_PIXELFORMAT_YV12       => 0x32315659,
    SDL_PIXELFORMAT_IYUV       => 0x56555949,
    SDL_PIXELFORMAT_YUY2       => 0x32595559,
    SDL_PIXELFORMAT_UYVY       => 0x59565955,
    SDL_PIXELFORMAT_YVYU       => 0x55595659,
    SDL_PIXELFORMAT_NV12       => 0x3231564e,
    SDL_PIXELFORMAT_NV21       => 0x3132564e,
    SDL_PIXELFORMAT_P010       => 0x30313050,
    SDL_PIXELFORMAT_EXTERNAL_OES => 0x2053454f
);


# For more see: "https://wiki.libsdl.org/SDL3/SDL_Texture"
our class SDL_Texture is repr('CStruct') is export
{
    # The format is a uint from the enum SDL_PixelFormat
    # We cannot directly use the raku enum in the struct so 
    # we use a uint instead.
    has uint32 $.format;
    has int64 $.w;
    has int64 $.h;
    has int64 $.refcount;
}

our class SDL_Vertex is repr('CStruct') is export 
{
    has SDL_FPoint $.position;
    has SDL_FColor $.color;
    has SDL_FPoint $.tex_coord;
};

# SDL_Renderer is an opaque pointer;
our class SDL_Renderer is repr('CPointer') is export {
    submethod DESTROY {
        SDL_DestroyRenderer(self);
    }
};
our class SDL_FRectPointer is repr('CPointer') is export {};

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateWindowAndRenderer"
our sub SDL_CreateWindowAndRenderer(Str, uint32, uint32, uint32, uint32) returns Bool is native($SDL-LIB, v0) is symbol('SDL_CreateWindowAndRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_CreateRenderer"
our sub SDL_CreateRenderer(Pointer, uint32) returns SDL_Renderer is native($SDL-LIB, v0) is symbol('SDL_CreateRenderer') is export { * }

our sub SDL_SetRenderDrawColor(SDL_Renderer, uint8, uint8, uint8, uint8) returns Bool is native($SDL-LIB, v0) is symbol('SDL_SetRenderDrawColor') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_SetRenderDrawColorFloat"
our sub SDL_SetRenderDrawColorFloat(SDL_Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB, v0) is symbol('SDL_SetRenderDrawColorFloat') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderGeometry"
our sub SDL_SDL_RenderGeometry() returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderGeometry') is export { * };

our sub SDL_RenderPresent(SDL_Renderer) is native($SDL-LIB, v0) is symbol('SDL_RenderPresent') is export { * }

our sub SDL_RenderClear(SDL_Renderer) is native($SDL-LIB, v0) is symbol('SDL_RenderClear') is export { * }

our sub SDL_DestroyRenderer(SDL_Renderer) is native($SDL-LIB, v0) is symbol('SDL_DestroyRenderer') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderRect"
our sub SDL_RenderRect(SDL_Renderer, SDL_FRect is rw) returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderRect') is export { * };
# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderFillRect"
our sub SDL_RenderFillRect(SDL_Renderer, SDL_FRect is rw) returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderFillRect') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoints"
our sub SDL_RenderPoints(SDL_Renderer, CArray[SDL_FPoint], int32) returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderPoints') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderPoint"
our sub SDL_RenderPoint(SDL_Renderer, num32, num32) returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderPoint') is export { * };

# For more see: "https://wiki.libsdl.org/SDL3/SDL_RenderLine"
our sub SDL_RenderLine(SDL_Renderer, num32, num32, num32, num32) returns Bool is native($SDL-LIB, v0) is symbol('SDL_RenderLine') is export { * }

# For more see: "https://wiki.libsdl.org/SDL3/SDL_GetRenderDriver"
our sub SDL_GetRenderDriver(int64 $index) returns Str is native($SDL-LIB, v0) is symbol('SDL_GetRenderDrivers') is export { * }