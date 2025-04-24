using System;
using System.Runtime.InteropServices;
using SDL3;

namespace AyanamisTower.NihilEx;

/// <summary>
/// Surface wrapper
/// </summary>
public class Surface
{
    /// <summary>
    /// Gets the native SDL surface handler
    /// </summary>
    public nint NativeSurfaceHandler { get; }

    /// <summary>
    /// Constructor used to wrap a native handler
    /// </summary>
    /// <param name="nativeSurfaceHandler"></param>
    public Surface(nint nativeSurfaceHandler)
    {
        NativeSurfaceHandler = nativeSurfaceHandler;
    }
    /*
    /// <summary>
    /// Constructor that creates a new surface with the specified dimensions and pixel format
    /// </summary>
    /// <param name="width">The width of the surface in pixels</param>
    /// <param name="height">The height of the surface in pixels</param>
    /// <param name="pixelFormat">The pixel format to use for the surface</param>
    public Surface(int width, int height, SDL.PixelFormat pixelFormat)
    {
        NativeSurfaceHandler = SDL.CreateSurface(width, height, pixelFormat);
        Marshal.PtrToStructure<SDL.Surface>(NativeSurfaceHandler);
    }

    /// <summary>
    /// Copies the surface data from the source to the destination
    /// </summary>
    /// <param name="destination">The surface to copy to</param>
    public void BlitSurface(Surface destination)
    {
        //copy the entire surface
        var successful = SDL.BlitSurface(NativeSurfaceHandler, IntPtr.Zero, destination.NativeSurfaceHandler, IntPtr.Zero);
        if (!successful)
        {
            throw new Exception($"SDL_ERROR: {SDL.GetError()}");
        }
    }
    */
}
