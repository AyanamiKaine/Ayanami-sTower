using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// Lightweight star texture generator used by the PlanetTextureGenerator demo.
// Produces a turbulent, emissive disk with optional spots and flares tuned
// to mimic spectral classes (O, B, A, F, G, K, M) plus RedGiant, WhiteDwarf,
// and NeutronStar.
/// <summary>
/// Generates a star texture based on the specified parameters.
/// </summary>
public static class StarTextureGenerator
{
    // Levels of detail used for saved star textures (lod0 full, lod1 half, etc.)
    const int LOD_COUNT = 4;

    /// <summary>
    /// Create LOD subfolders and generate star textures at multiple resolutions.
    /// Writes files named with their resolution (e.g. name_2048x1024.png) and
    /// will also create cubemap subfolders when requested.
    /// </summary>
    public static void GenerateStarTextureWithLods(int seed, string typeName, string outputPath, int width = 2048, int height = 1024, bool cubeMap = false, int faceSize = 1024, bool generateBoth = false)
    {
        string parentDir = Path.GetDirectoryName(outputPath) ?? ".";
        string baseName = Path.GetFileNameWithoutExtension(outputPath);
        string textureDir = Path.Combine(parentDir, baseName);
        Directory.CreateDirectory(textureDir);

        for (int lod = 0; lod < LOD_COUNT; lod++)
        {
            string lodFolder = Path.Combine(textureDir, $"lod{lod}");
            Directory.CreateDirectory(lodFolder);

            int divisor = 1 << lod; // 1,2,4,8
            int lodW = Math.Max(1, width / divisor);
            int lodH = Math.Max(1, height / divisor);
            int lodFace = Math.Max(1, faceSize / divisor);

            string fileName = Path.GetFileNameWithoutExtension(outputPath);
            string ext = Path.GetExtension(outputPath);
            string outFileName = $"{fileName}_{lodW}x{lodH}{ext}";
            string outFile = Path.Combine(lodFolder, outFileName);

            Console.WriteLine($"  Star LOD{lod}: writing {outFile} ({lodW}x{lodH})");
            GenerateStarTexture(seed, typeName, outFile, lodW, lodH, cubeMap, lodFace);
        }
    }
    static Rgba32 Lerp(Rgba32 a, Rgba32 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte r = (byte)(a.R + ((b.R - a.R) * t));
        byte g = (byte)(a.G + ((b.G - a.G) * t));
        byte bl = (byte)(a.B + ((b.B - a.B) * t));
        return new Rgba32(r, g, bl);
    }

    static Rgba32 Mul(Rgba32 c, float m)
    {
        int ir = (int)MathF.Round(c.R * m);
        int ig = (int)MathF.Round(c.G * m);
        int ib = (int)MathF.Round(c.B * m);
        ir = Math.Clamp(ir, 0, 255);
        ig = Math.Clamp(ig, 0, 255);
        ib = Math.Clamp(ib, 0, 255);
        return new Rgba32((byte)ir, (byte)ig, (byte)ib);
    }

    static Rgba32 Add(Rgba32 a, Rgba32 b)
    {
        int ir = a.R + b.R;
        int ig = a.G + b.G;
        int ib = a.B + b.B;
        ir = Math.Clamp(ir, 0, 255);
        ig = Math.Clamp(ig, 0, 255);
        ib = Math.Clamp(ib, 0, 255);
        return new Rgba32((byte)ir, (byte)ig, (byte)ib);
    }

    /// <summary>
    /// typeName is the stringified enum used by the caller (e.g. "Star_G", "RedGiant").
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="typeName"></param>
    /// <param name="outputPath"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="cubeMap">If true, generates six cube-face PNGs instead of a single equirectangular texture. The files will be saved as &lt;outputPathWithoutExt&gt;_PX.png/_NX.png/_PY.png/_NY.png/_PZ.png/_NZ.png.</param>
    /// <param name="faceSize">When <paramref name="cubeMap"/> is true, this is the size (width/height) of each cube face. Otherwise ignored.</param>
    public static void GenerateStarTexture(int seed, string typeName, string outputPath, int width = 2048, int height = 1024, bool cubeMap = false, int faceSize = 1024)
    {
        var rng = new Random(seed);

        // Map typeName -> visual parameters (surface temperature tint and noise tuning)
        Rgba32 baseColor = new Rgba32(255, 244, 214); // default G
        float brightness = 1.0f;
        float convectionScale = 1.0f / 80f; // 3D noise frequency controlling convection cell size
        float granulationScale = 1.0f / 6f; // small-scale noise
        bool spots = false;

        string t = typeName ?? string.Empty;
        if (t.StartsWith("Star_", StringComparison.OrdinalIgnoreCase))
        {
            var s = t.Substring(5).ToUpperInvariant();
            switch (s)
            {
                case "O": baseColor = new Rgba32(200, 220, 255); brightness = 2.0f; convectionScale = 1.0f / 160f; granulationScale = 1.0f / 10f; break;
                case "B": baseColor = new Rgba32(190, 210, 255); brightness = 1.7f; convectionScale = 1.0f / 130f; granulationScale = 1.0f / 9f; break;
                case "A": baseColor = new Rgba32(230, 240, 255); brightness = 1.4f; convectionScale = 1.0f / 110f; granulationScale = 1.0f / 8f; break;
                case "F": baseColor = new Rgba32(255, 250, 240); brightness = 1.15f; convectionScale = 1.0f / 95f; granulationScale = 1.0f / 7f; break;
                case "G": baseColor = new Rgba32(255, 244, 214); brightness = 1.0f; convectionScale = 1.0f / 80f; granulationScale = 1.0f / 6f; spots = true; break;
                case "K": baseColor = new Rgba32(255, 205, 150); brightness = 0.85f; convectionScale = 1.0f / 65f; granulationScale = 1.0f / 5f; spots = true; break;
                case "M": baseColor = new Rgba32(255, 120, 90); brightness = 0.6f; convectionScale = 1.0f / 40f; granulationScale = 1.0f / 4f; spots = true; break;
                default: break;
            }
        }
        else if (t.Equals("RedGiant", StringComparison.OrdinalIgnoreCase))
        {
            baseColor = new Rgba32(220, 100, 60);
            brightness = 0.9f;
            convectionScale = 1.0f / 40f;
            granulationScale = 1.0f / 3f;
            spots = true;
        }
        else if (t.Equals("WhiteDwarf", StringComparison.OrdinalIgnoreCase))
        {
            baseColor = new Rgba32(245, 255, 255);
            brightness = 3.5f;
            convectionScale = 1.0f / 250f;
            granulationScale = 1.0f / 12f;
            spots = false;
        }
        else if (t.Equals("NeutronStar", StringComparison.OrdinalIgnoreCase))
        {
            baseColor = new Rgba32(180, 220, 255);
            brightness = 6.0f;
            convectionScale = 1.0f / 800f;
            granulationScale = 1.0f / 40f;
            spots = false;
        }

        // Convection noise (3D on sphere)
        var convNoise = new FastNoiseLite();
        convNoise.SetSeed(seed);
        convNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        convNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        convNoise.SetFractalOctaves(4);
        convNoise.SetFrequency(convectionScale);

        // Granulation noise (adds small-scale detail)
        var granNoise = new FastNoiseLite();
        granNoise.SetSeed(seed ^ unchecked((int)0x9E3779B9));
        granNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        granNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        granNoise.SetFractalOctaves(3);
        granNoise.SetFrequency(granulationScale);

        // Spot noise (low frequency cellular for dark spots)
        var spotNoise = new FastNoiseLite();
        spotNoise.SetSeed(seed ^ unchecked((int)0x27d4eb2d));
        spotNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        spotNoise.SetFrequency(1.0f / 120f);

        // Surface noise (fine, high-frequency detail to add subtle texture)
        var surfaceNoise = new FastNoiseLite();
        surfaceNoise.SetSeed(seed ^ unchecked((int)0x7fffffff));
        surfaceNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        surfaceNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        surfaceNoise.SetFractalOctaves(2);
        // Frequency is relative to granulationScale so it adapts per star type
        surfaceNoise.SetFrequency(granulationScale * 6.0f);

        if (!cubeMap)
        {
            using var img = new Image<Rgba32>(width, height);

            for (int px = 0; px < width; px++)
            {
                // longitude [0..2PI)
                float lon = (px / (float)width) * MathF.PI * 2f;
                for (int py = 0; py < height; py++)
                {
                    // latitude [-PI/2 .. PI/2]
                    float lat = ((py / (float)height) * MathF.PI) - (MathF.PI * 0.5f);

                    // convert to 3D unit vector on sphere
                    float cx = MathF.Cos(lat) * MathF.Cos(lon);
                    float cy = MathF.Sin(lat);
                    float cz = MathF.Cos(lat) * MathF.Sin(lon);

                    // sample 3D convection noise
                    float conv = convNoise.GetNoise(cx, cy, cz); // [-1,1]
                    conv = (conv + 1f) * 0.5f; // [0,1]

                    // sample granulation at higher frequency
                    float gran = granNoise.GetNoise(cx * 2.0f, cy * 2.0f, cz * 2.0f);
                    gran = (gran + 1f) * 0.5f;

                    // combine layers
                    float intensity = conv * 0.75f + gran * 0.25f;

                    // spots reduce intensity in localized regions
                    if (spots)
                    {
                        float s = spotNoise.GetNoise(cx * 0.8f, cy * 0.8f, cz * 0.8f);
                        s = (s + 1f) * 0.5f;
                        // threshold to create darker regions
                        if (s < 0.35f)
                        {
                            float dark = 1f - (0.35f - s) * 1.6f; // darkening factor
                            intensity *= Math.Clamp(dark, 0.45f, 1f);
                        }
                    }

                    // subtle latitude-dependent brightness (hotter poles effect optional)
                    float latBoost = 1.0f + (0.03f * MathF.Cos(lat * 2f));
                    intensity *= latBoost;

                    // apply overall brightness and gamma
                    float final = MathF.Pow(intensity * brightness, 0.9f);

                    // add a subtle, high-frequency surface noise layer to give the
                    // star a bit more perceived surface texture without changing
                    // the overall brightness much. Sample the 3D surfaceNoise and
                    // convert it to a small additive offset.
                    float surf = surfaceNoise.GetNoise(cx * 3.0f, cy * 3.0f, cz * 3.0f); // [-1,1]
                    surf = (surf + 1f) * 0.5f; // [0,1]
                    // map to a small bipolar offset around 0 -> [-amp, +amp]
                    const float surfaceAmp = 0.08f; // max +/- amplitude (tweakable)
                    float surfOffset = (surf - 0.5f) * 2f * surfaceAmp; // [-amp,amp]

                    final = Math.Clamp(final + surfOffset, 0f, 8f);

                    // colorize by base color
                    var col = Mul(baseColor, final);

                    img[px, py] = col;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            img.SaveAsPng(outputPath);
        }
        else
        {
            // Cube map generation: write six square faces named with suffixes
            // PX, NX, PY, NY, PZ, NZ (positive/negative X/Y/Z).
            string parentDir = Path.GetDirectoryName(outputPath) ?? ".";
            string baseName = Path.GetFileNameWithoutExtension(outputPath);
            // create a dedicated subfolder for the generated cubemap faces
            string cubemapDir = Path.Combine(parentDir, baseName + "_cubemap");
            Directory.CreateDirectory(cubemapDir);

            string[] faceSuffix = new[] { "PX", "NX", "PY", "NY", "PZ", "NZ" };

            for (int face = 0; face < 6; face++)
            {
                using var imgFace = new Image<Rgba32>(faceSize, faceSize);

                for (int px = 0; px < faceSize; px++)
                {
                    // uv in [-1,1], center of texel
                    float u = (2f * ((px + 0.5f) / faceSize)) - 1f;
                    for (int py = 0; py < faceSize; py++)
                    {
                        float v = (2f * ((py + 0.5f) / faceSize)) - 1f;

                        // map (u,v) to 3D direction for current cube face
                        float cx, cy, cz;
                        switch (face)
                        {
                            // +X
                            case 0: cx = 1f; cy = v; cz = -u; break;
                            // -X
                            case 1: cx = -1f; cy = v; cz = u; break;
                            // +Y
                            case 2: cx = u; cy = 1f; cz = -v; break;
                            // -Y
                            case 3: cx = u; cy = -1f; cz = v; break;
                            // +Z
                            case 4: cx = u; cy = v; cz = 1f; break;
                            // -Z
                            default: cx = -u; cy = v; cz = -1f; break;
                        }

                        // normalize
                        float len = MathF.Sqrt(cx * cx + cy * cy + cz * cz);
                        cx /= len; cy /= len; cz /= len;

                        // sample 3D convection noise
                        float conv = convNoise.GetNoise(cx, cy, cz); // [-1,1]
                        conv = (conv + 1f) * 0.5f; // [0,1]

                        // sample granulation at higher frequency
                        float gran = granNoise.GetNoise(cx * 2.0f, cy * 2.0f, cz * 2.0f);
                        gran = (gran + 1f) * 0.5f;

                        // combine layers
                        float intensity = conv * 0.75f + gran * 0.25f;

                        // spots reduce intensity in localized regions
                        if (spots)
                        {
                            float s = spotNoise.GetNoise(cx * 0.8f, cy * 0.8f, cz * 0.8f);
                            s = (s + 1f) * 0.5f;
                            // threshold to create darker regions
                            if (s < 0.35f)
                            {
                                float dark = 1f - (0.35f - s) * 1.6f; // darkening factor
                                intensity *= Math.Clamp(dark, 0.45f, 1f);
                            }
                        }

                        // latitude-dependent boost isn't meaningful per-face; use a
                        // small pole-like boost based on absolute Y to preserve look.
                        float latBoost = 1.0f + (0.03f * MathF.Abs(cy));
                        intensity *= latBoost;

                        // apply overall brightness and gamma
                        float final = MathF.Pow(intensity * brightness, 0.9f);

                        // surface noise
                        float surf = surfaceNoise.GetNoise(cx * 3.0f, cy * 3.0f, cz * 3.0f); // [-1,1]
                        surf = (surf + 1f) * 0.5f; // [0,1]
                        const float surfaceAmp = 0.08f;
                        float surfOffset = (surf - 0.5f) * 2f * surfaceAmp; // [-amp,amp]
                        final = Math.Clamp(final + surfOffset, 0f, 8f);

                        var col = Mul(baseColor, final);
                        imgFace[px, py] = col;
                    }
                }

                string outFile = Path.Combine(cubemapDir, baseName + "_" + faceSuffix[face] + ".png");
                imgFace.SaveAsPng(outFile);
            }
        }
    }
}
