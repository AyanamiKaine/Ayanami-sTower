using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Assets;

/// <summary>
/// Manages the loading and unloading of game assets.
/// </summary>
public static class AssetManager
{
    /// <summary>
    /// The name of the directory of the game assets
    /// </summary>
    public const string AssetFolderName = "GameAssets";

    // Normalize to TitleStorage-friendly relative POSIX path
    private static string NormalizeTitlePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
        {
            var baseDir = AppContext.BaseDirectory.Replace('\\', '/');
            if (normalized.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(baseDir.Length);
            }
            else
            {
                // Leave as-is; TitleStorage calls will fail and log
            }
        }
        return normalized.TrimStart('/');
    }

    /// <summary>
    /// Creates a cubemap texture from six images. Expected order: +X, -X, +Y, -Y, +Z, -Z.
    /// All images must be square and same size. Returns null on failure.
    /// </summary>
    public static Texture? CreateCubemapFromSixFiles(Game game, string posX, string negX, string posY, string negY, string posZ, string negZ)
    {
        string[] input =
        [
            NormalizeTitlePath(posX),
                NormalizeTitlePath(negX),
                NormalizeTitlePath(posY),
                NormalizeTitlePath(negY),
                NormalizeTitlePath(posZ),
                NormalizeTitlePath(negZ)
        ];

        // Validate existence and gather dimensions
        uint size = 0;
        for (int i = 0; i < 6; i++)
        {
            // GetFileSize returns true on success; treat a false return as "missing".
            if (!game.RootTitleStorage.GetFileSize(input[i], out _))
            {
                Console.WriteLine($"[CreateCubemap] Missing file: {input[i]}");
                return null;
            }
            if (!MoonWorks.Graphics.ImageUtils.ImageInfoFromFile(game.RootTitleStorage, input[i], out var w, out var h, out _))
            {
                Console.WriteLine($"[CreateCubemap] Unsupported or corrupt image: {input[i]}");
                return null;
            }
            if (w != h)
            {
                Console.WriteLine($"[CreateCubemap] Image is not square: {input[i]} ({w}x{h})");
                return null;
            }
            if (i == 0) { size = w; }
            else if (w != size || h != size)
            {
                Console.WriteLine($"[CreateCubemap] Image size mismatch: {input[i]} ({w}x{h}) expected {size}x{size}");
                return null;
            }
        }

        using var uploader = new ResourceUploader(game.GraphicsDevice);
        var cube = Texture.CreateCube(game.GraphicsDevice, "Cubemap", size, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler, levelCount: 1);
        if (cube == null)
        {
            Console.WriteLine("[CreateCubemap] Failed to create cube texture");
            return null;
        }

        for (int face = 0; face < 6; face++)
        {
            var region = new TextureRegion
            {
                Texture = cube.Handle,
                Layer = (uint)face,
                MipLevel = 0,
                X = 0,
                Y = 0,
                Z = 0,
                W = size,
                H = size,
                D = 1
            };
            uploader.SetTextureDataFromCompressed(game.RootTitleStorage, input[face], region);
        }

        uploader.UploadAndWait();
        return cube;
    }

    /// <summary>
    /// Loads an image file (PNG, JPG, BMP, etc.) or a DDS file and uploads it as a GPU texture.
    /// For standard image formats, data is decoded to RGBA8 and uploaded as a 2D texture.
    /// For DDS, existing mip levels and cube faces are preserved.
    /// Returns null if the file could not be read or decoded.
    /// </summary>
    public static Texture? LoadTextureFromFile(Game game, string path)
    {
        // Normalize to TitleStorage-friendly relative POSIX path
        var normalized = NormalizeTitlePath(path);

        // Quick existence + file size check. GetFileSize returns true on success.
        if (!game.RootTitleStorage.GetFileSize(normalized, out var fileSize))
        {
            Console.WriteLine($"[LoadTextureFromFile] File not found in TitleStorage: {normalized}");
            return null;
        }

        // Choose loader by file extension
        var ext = Path.GetExtension(normalized).ToLowerInvariant();

        using var uploader = new ResourceUploader(game.GraphicsDevice);
        Texture? texture = null;
        var name = Path.GetFileNameWithoutExtension(normalized);

        // Try to obtain basic image info (width/height). If this fails, prefer the explicit StbImageSharp fallback
        bool haveImageInfo = false;
        int imgW = 0, imgH = 0;
        if (MoonWorks.Graphics.ImageUtils.ImageInfoFromFile(game.RootTitleStorage, normalized, out var w, out var h, out _))
        {
            imgW = (int)w;
            imgH = (int)h;
            haveImageInfo = true;
            if (imgW < 1 || imgH < 1)
            {
                Console.WriteLine($"[LoadTextureFromFile] Image has invalid dimensions ({imgW}x{imgH}): {normalized}");
                return null;
            }
        }

        if (ext == ".dds")
        {
            texture = uploader.CreateTextureFromDDS(name, game.RootTitleStorage, normalized);
            if (texture == null)
            {
                Console.WriteLine($"[LoadTextureFromFile] CreateTextureFromDDS failed for: {normalized}");
                return null;
            }
        }
        else
        {
            // Only attempt the compressed-path uploader if we successfully read image dimensions.
            // Some backends may create an invalid/zero-sized texture if fed an unknown container.
            if (haveImageInfo)
            {
                texture = uploader.CreateTexture2DFromCompressed(
                    name,
                    game.RootTitleStorage,
                    normalized,
                    TextureFormat.R8G8B8A8Unorm,
                    TextureUsageFlags.Sampler
                );

                if (texture == null)
                {
                    Console.WriteLine($"[LoadTextureFromFile] CreateTexture2DFromCompressed returned null for: {normalized}, falling back to software decode");
                }
                else
                {
                    // We trust ImageInfoFromFile here; assume texture dimensions match. If you still see assertions,
                    // we'll fall back below when texture is null.
                }
            }

            if (texture == null)
            {
                // Fallback: try StbImageSharp to decode common formats (JPEG/PNG/etc.)
                try
                {
                    if (fileSize == 0)
                    {
                        Console.WriteLine($"[LoadTextureFromFile] Fallback: file is empty: {normalized}");
                        return null;
                    }

                    var bytes = new byte[fileSize];
                    // ReadFile returns true on success; treat a false return as failure.
                    if (!game.RootTitleStorage.ReadFile(normalized, bytes))
                    {
                        Console.WriteLine($"[LoadTextureFromFile] Fallback: failed to read file: {normalized}");
                        return null;
                    }

                    // Decode to RGBA using StbImageSharp
                    var img = StbImageSharp.ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                    if (img == null || img.Data == null || img.Width <= 0 || img.Height <= 0)
                    {
                        Console.WriteLine($"[LoadTextureFromFile] Fallback: decode failed or invalid dimensions for: {normalized}");
                        return null;
                    }

                    texture = uploader.CreateTexture2D<byte>(
                        name,
                        img.Data,
                        TextureFormat.R8G8B8A8Unorm,
                        TextureUsageFlags.Sampler,
                        (uint)img.Width,
                        (uint)img.Height
                    );

                    if (texture == null)
                    {
                        Console.WriteLine($"[LoadTextureFromFile] Fallback: texture creation failed for: {normalized}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoadTextureFromFile] Fallback exception for {normalized}: {ex.Message}");
                    return null;
                }
            }
        }

        // Final defensive validation
        if (texture == null)
        {
            Console.WriteLine($"[LoadTextureFromFile] Failed to create texture for: {normalized}");
            return null;
        }

        uploader.UploadAndWait();
        return texture;
    }
    /// <summary>
    /// Creates a solid color texture.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static Texture CreateSolidTexture(Game game, byte r, byte g, byte b, byte a)
    {
        using var uploader = new ResourceUploader(game.GraphicsDevice, 4);
        var tex = uploader.CreateTexture2D(
            "Solid",
            [r, g, b, a],
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler,
            1, 1);
        uploader.UploadAndWait();
        return tex;
    }

    /// <summary>
    /// Creates a checkerboard texture. Used as placeholder for missing textures.
    /// </summary>
    public static Texture CreateCheckerboardTexture(Game game, uint width, uint height, int cells, (byte r, byte g, byte b, byte a) c0, (byte r, byte g, byte b, byte a) c1)
    {
        int w = (int)width, h = (int)height;
        var data = new byte[w * h * 4];
        int cellW = Math.Max(1, w / cells);
        int cellH = Math.Max(1, h / cells);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool useC0 = ((x / cellW) + (y / cellH)) % 2 == 0;
                var (r, g, b, a) = useC0 ? c0 : c1;
                int i = ((y * w) + x) * 4;
                data[i + 0] = r;
                data[i + 1] = g;
                data[i + 2] = b;
                data[i + 3] = a;
            }
        }
        using var uploader = new ResourceUploader(game.GraphicsDevice, (uint)data.Length);
        var tex = uploader.CreateTexture2D<byte>(
            "Checker",
            data,
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler,
            width, height);
        uploader.UploadAndWait();
        return tex;
    }
}
