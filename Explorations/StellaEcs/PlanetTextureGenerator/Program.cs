// Import the necessary namespaces from the libraries we added.
using System;
using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// Small utility to generate many planet textures with different presets.
static class PlanetGenerator
{
    // Defaults
    const int DEFAULT_WIDTH = 1024 * 2;
    const int DEFAULT_HEIGHT = 512 * 2;

    enum PlanetType
    {
        EarthLike,
        Desert,
        Ice,
        GasGiant,
        Lava,
        OceanWorld,
        Rocky,
        Metallic,
        IronRich,
        CarbonRich,
        Volcanic,
        Tectonic,
        Barren,
        ChlorophyllRich,
        // New celestial bodies
        Asteroid,
        Comet,
        Moon,
        Star_O,
        Star_B,
        Star_A,
        Star_F,
        Star_G,
        Star_K,
        Star_M,
        RedGiant,
        WhiteDwarf,
        NeutronStar
    }

    // A simple color ramp delegate: given a height value [0..1] return a color.
    delegate Rgba32 ColorRamp(float h);

    static readonly Random GlobalRng = new();

    // Preset that controls noise and overlay parameters for a planet type.
    class PlanetPreset
    {
        public FastNoiseLite.NoiseType NoiseType;
        public FastNoiseLite.FractalType FractalType;
        public float Frequency;
        public int Octaves;
        public float Lacunarity;
        public float Gain;

        // overlays
        public float CloudScale = 20f;
        public int CloudOctaves = 3;
        public float CloudThreshold = 0.48f;

        public float CraterScale = 8f;
        public float CraterThreshold = 0.6f;

        public float RustScale = 30f;
        public float RustAmount = 0.6f;

        public float BandStrength = 0.6f;

        // visual tweaking
        public float Brightness = 1f;
        public float Contrast = 1f;
    }

    static PlanetPreset GetPlanetPreset(PlanetType type, int seed)
    {
        var rng = new Random(seed);
        float jitter() => 1f + (float)(rng.NextDouble() * 0.2 - 0.1);

        var p = new PlanetPreset();
        switch (type)
        {
            case PlanetType.EarthLike:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (150f * jitter());
                p.Octaves = 6;
                p.Lacunarity = 2.0f;
                p.Gain = 0.5f;
                p.CloudScale = 20f * jitter();
                p.CloudOctaves = 3;
                break;
            case PlanetType.Desert:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (200f * jitter());
                p.Octaves = 5;
                p.Lacunarity = 2.2f;
                p.Gain = 0.45f;
                p.CloudScale = 40f;
                break;
            case PlanetType.Ice:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (120f * jitter());
                p.Octaves = 5;
                p.Gain = 0.6f;
                p.CloudScale = 18f;
                break;
            case PlanetType.GasGiant:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.None;
                p.Frequency = 1.0f / (60f * jitter());
                p.Octaves = 1;
                p.BandStrength = 0.7f + (float)rng.NextDouble() * 0.3f;
                break;
            case PlanetType.Lava:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (60f * jitter());
                p.Octaves = 3;
                p.Gain = 0.6f;
                break;
            case PlanetType.Volcanic:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (80f * jitter());
                p.Octaves = 4;
                p.Gain = 0.55f;
                p.RustScale = 10f;
                break;
            case PlanetType.OceanWorld:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (220f * jitter());
                p.Octaves = 3;
                p.Gain = 0.5f;
                p.CloudScale = 12f;
                break;
            case PlanetType.Rocky:
            case PlanetType.Barren:
            case PlanetType.Asteroid:
            case PlanetType.Moon:
                p.NoiseType = FastNoiseLite.NoiseType.Cellular;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (90f * jitter());
                p.Octaves = 6;
                p.Gain = 0.5f;
                p.CraterScale = 6f;
                p.CraterThreshold = 0.58f + (float)rng.NextDouble() * 0.05f;
                p.RustAmount = 0.5f;
                break;
            case PlanetType.Comet:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (40f * jitter());
                p.Octaves = 3;
                p.Gain = 0.7f;
                p.CraterScale = 4f;
                p.CraterThreshold = 0.65f;
                break;
            case PlanetType.Metallic:
            case PlanetType.IronRich:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (70f * jitter());
                p.Octaves = 5;
                p.RustScale = 18f;
                p.RustAmount = 0.8f;
                break;
            case PlanetType.CarbonRich:
                p.NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (100f * jitter());
                p.Octaves = 4;
                break;
            case PlanetType.Tectonic:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.Ridged;
                p.Frequency = 1.0f / (110f * jitter());
                p.Octaves = 6;
                p.Lacunarity = 2.1f;
                break;
            case PlanetType.ChlorophyllRich:
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (140f * jitter());
                p.Octaves = 5;
                break;
            default: // Should not be reached, but as a fallback
                p.NoiseType = FastNoiseLite.NoiseType.Perlin;
                p.FractalType = FastNoiseLite.FractalType.FBm;
                p.Frequency = 1.0f / (150f * jitter());
                p.Octaves = 5;
                break;
        }

        // small random tweaks
        p.Lacunarity = p.Lacunarity == 0 ? 2f : p.Lacunarity * (1f + (float)(rng.NextDouble() * 0.2 - 0.1));
        p.Gain = p.Gain == 0 ? 0.5f : p.Gain * (1f + (float)(rng.NextDouble() * 0.2 - 0.1));

        return p;
    }

    // Public contract: generate a single texture file with the chosen parameters.
    // Inputs: seed, type, output path, width/height. Output: PNG file written. Errors: throws on IO.
    static void GeneratePlanetTexture(int seed, PlanetType type, string outputPath, int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT)
    {
        // Configure noise via preset
        var preset = GetPlanetPreset(type, seed);
        var noise = new FastNoiseLite();
        noise.SetSeed(seed);
        noise.SetNoiseType(preset.NoiseType);
        noise.SetFrequency(preset.Frequency);
        noise.SetFractalType(preset.FractalType);
        noise.SetFractalOctaves(preset.Octaves);
        noise.SetFractalLacunarity(preset.Lacunarity);
        noise.SetFractalGain(preset.Gain);

        // Choose a color ramp for the planet type
        ColorRamp ramp = type switch
        {
            PlanetType.EarthLike => EarthLikeRamp,
            PlanetType.Desert => DesertRamp,
            PlanetType.Ice => IceRamp,
            PlanetType.GasGiant => GasGiantRamp,
            PlanetType.Lava => LavaRamp,
            PlanetType.OceanWorld => OceanRamp,
            PlanetType.Rocky => RockyRamp,
            PlanetType.Metallic => MetallicRamp,
            PlanetType.IronRich => IronRichRamp,
            PlanetType.CarbonRich => CarbonRichRamp,
            PlanetType.Volcanic => VolcanicRamp,
            PlanetType.Tectonic => TectonicRamp,
            PlanetType.Barren => RockyRamp, // Barren can reuse Rocky
            PlanetType.ChlorophyllRich => ChlorophyllRichRamp,
            PlanetType.Asteroid => RockyRamp, // Asteroids are essentially rocks
            PlanetType.Comet => CometRamp,
            PlanetType.Moon => RockyRamp, // Moons are also rocky
            _ => EarthLikeRamp // Fallback, should ideally not be used
        };

        // Additional noise layers for clouds, craters and rust/oxidation overlays.
        var cloudNoise = new FastNoiseLite();
        cloudNoise.SetSeed(seed ^ unchecked((int)0x9E3779B9));
        cloudNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        cloudNoise.SetFrequency(1.0f / preset.CloudScale);
        cloudNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        cloudNoise.SetFractalOctaves(preset.CloudOctaves);

        var craterNoise = new FastNoiseLite();
        craterNoise.SetSeed(seed ^ unchecked((int)0x27d4eb2d));
        craterNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        craterNoise.SetFrequency(1.0f / preset.CraterScale);

        var rustNoise = new FastNoiseLite();
        rustNoise.SetSeed(seed ^ unchecked((int)0x85ebca6b));
        rustNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        rustNoise.SetFrequency(1.0f / preset.RustScale);

        using var img = new Image<Rgba32>(width, height);

        // For Earth-like storms: create some storm centers determined by seed
        var rng = new Random(seed);
        var stormCenters = new List<(float x, float y, float radius)>();
        if (type == PlanetType.EarthLike)
        {
            int storms = 2 + (rng.Next() % 3);
            for (int s = 0; s < storms; s++)
            {
                stormCenters.Add(((float)rng.NextDouble(), (float)rng.NextDouble(), 0.05f + (float)rng.NextDouble() * 0.12f));
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Map pixel coordinates to a noise coordinate space
                float nx = x / (float)width;
                float ny = y / (float)height;

                // Sample coordinates
                float sampleX = nx * width;
                float sampleY = ny * height;

                float n = noise.GetNoise(sampleX, sampleY); // [-1,1]
                float h = (n + 1f) * 0.5f; // [0,1]

                // For gas giants, overlay banded noise
                if (type == PlanetType.GasGiant)
                {
                    // create latitudinal bands by using y coordinate
                    float band = (float)Math.Abs(Math.Sin(ny * Math.PI * (1.0 + (seed % 5))));
                    float bandNoise = (noise.GetNoise(sampleX * 0.4f, sampleY * 0.4f) * 0.5f) + 0.5f;
                    h = Math.Clamp((band * preset.BandStrength) + (bandNoise * (1f - preset.BandStrength)), 0f, 1f);
                }

                // Base color from height ramp
                var baseColor = ramp(h);
                var finalColor = baseColor;

                // --- Overlays ---

                // --- Clouds ---
                // Only add thick visible clouds for planets with significant atmospheres.
                if (type == PlanetType.EarthLike || type == PlanetType.OceanWorld)
                {
                    float c = cloudNoise.GetNoise(sampleX * 0.06f, sampleY * 0.06f); // [-1,1]
                    float cloudAlpha = Math.Clamp((c + 1f) * 0.5f - preset.CloudThreshold, 0f, 1f);
                    // soften and reduce in high elevation
                    cloudAlpha *= 1f - MathF.Pow(h, 1.5f);
                    cloudAlpha = MathF.Pow(cloudAlpha, 0.8f);
                    if (cloudAlpha > 0.01f)
                    {
                        var cloudColor = new Rgba32(250, 250, 250);
                        finalColor = Lerp(finalColor, cloudColor, cloudAlpha * 0.9f);
                    }
                }

                // --- Storms (EarthLike) ---
                if (type == PlanetType.EarthLike && stormCenters.Count > 0)
                {
                    foreach (var (sx, sy, sr) in stormCenters)
                    {
                        float dx = nx - sx;
                        float dy = ny - sy;
                        // wrap horizontally
                        if (dx > 0.5f) dx -= 1f;
                        if (dx < -0.5f) dx += 1f;
                        float dist = MathF.Sqrt(dx * dx + dy * dy);
                        if (dist < sr)
                        {
                            // stronger at center, softer on edges
                            float t = 1f - (dist / sr);
                            t = MathF.Pow(t, 1.5f);
                            // storm color : white with bluish tint
                            var stormCol = new Rgba32(240, 245, 255);
                            finalColor = Lerp(finalColor, stormCol, Math.Clamp(t * 0.85f, 0f, 1f));
                        }
                    }
                }

                // --- Gas giant major storm (e.g., Great Red Spot) ---
                if (type == PlanetType.GasGiant)
                {
                    // create one major storm per seed occasionally
                    float gx = (float)((seed * 9301 + 49297) % 1000) / 1000f;
                    float gy = 0.5f + (float)Math.Sin(seed % 37) * 0.12f;
                    float dx = nx - gx;
                    if (dx > 0.5f) dx -= 1f;
                    float dy = ny - gy;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist < 0.12f)
                    {
                        float t = 1f - (dist / 0.12f);
                        t = MathF.Pow(t, 1.2f);
                        var gStorm = new Rgba32(210, 90, 60);
                        finalColor = Lerp(finalColor, gStorm, Math.Clamp(t * 0.9f, 0f, 1f));
                    }
                }

                // --- Rust / oxidation overlay for relevant bodies ---
                if (type == PlanetType.Rocky || type == PlanetType.IronRich || type == PlanetType.Metallic)
                {
                    float r = rustNoise.GetNoise(sampleX * 0.08f, sampleY * 0.08f);
                    float rustMask = Math.Clamp((r + 1f) * 0.5f, 0f, 1f);
                    // bias so rust appears in patches
                    rustMask = MathF.Pow(rustMask, 1.3f) * preset.RustAmount;
                    var rustColor = new Rgba32(170, 80, 40);
                    finalColor = Lerp(finalColor, rustColor, rustMask);
                }

                // --- Craters for bodies with no atmosphere to erode them ---
                if (type == PlanetType.Rocky || type == PlanetType.Barren || type == PlanetType.Metallic || type == PlanetType.Moon || type == PlanetType.Asteroid || type == PlanetType.Comet)
                {
                    float cr = craterNoise.GetNoise(sampleX * 1.2f, sampleY * 1.2f);
                    // Cellular noise produces spots; threshold to carve craters
                    if (cr > preset.CraterThreshold)
                    {
                        float amt = (cr - preset.CraterThreshold) / (1f - preset.CraterThreshold);
                        amt = MathF.Pow(amt, 2f);
                        var craterDark = new Rgba32(30, 22, 18);
                        finalColor = Lerp(finalColor, craterDark, Math.Clamp(amt * 0.6f, 0f, 1f));
                    }
                }

                img[x, y] = finalColor;
            }
        }
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        img.SaveAsPng(outputPath);
    }

    // Randomized batch generator. Creates `count` textures into `outDir`.
    static void GenerateRandomTextures(int count, string outDir, int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT)
    {
        Directory.CreateDirectory(outDir);
        var types = (PlanetType[])Enum.GetValues(typeof(PlanetType));
        for (int i = 0; i < count; i++)
        {
            int seed = GlobalRng.Next();
            var type = types[GlobalRng.Next(types.Length)];
            string filename = Path.Combine(outDir, $"planet_{type.ToString().ToLower()}_{seed}.png");
            Console.WriteLine($"Generating {filename} (type={type}, seed={seed})");
            try
            {
                // Star generation removed: skip star types.
                if (type.ToString().StartsWith("Star_") || type == PlanetType.RedGiant || type == PlanetType.WhiteDwarf || type == PlanetType.NeutronStar)
                {
                    // Route star/stellar-type generation to the star generator
                    string tname = type.ToString();
                    string starOut = Path.Combine(outDir, $"star_{tname.ToLower()}_{seed}.png");
                    Console.WriteLine($"Generating star texture {starOut} (type={type}, seed={seed})");
                    try
                    {
                        StarTextureGenerator.GenerateStarTexture(seed, tname, starOut, width, height);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to generate star {starOut}: {ex.Message}");
                    }
                    continue;
                }
                GeneratePlanetTexture(seed, type, filename, width, height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate {filename}: {ex.Message}");
            }
        }
    }

    // Star texture generator with a more realistic, turbulent surface.
    // GenerateStarTexture removed.

    // ---------------- Color ramps ----------------
    static Rgba32 Lerp(Rgba32 a, Rgba32 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte r = (byte)(a.R + ((b.R - a.R) * t));
        byte g = (byte)(a.G + ((b.G - a.G) * t));
        byte bl = (byte)(a.B + ((b.B - a.B) * t));
        return new Rgba32(r, g, bl);
    }

    static Rgba32 EarthLikeRamp(float h)
    {
        var deep = new Rgba32(0, 50, 150);
        var shallow = new Rgba32(60, 120, 180);
        var beach = new Rgba32(238, 214, 175);
        var land = new Rgba32(34, 139, 34);
        var mountain = new Rgba32(120, 110, 100);
        var snow = new Rgba32(255, 250, 250);

        if (h < 0.38f) return deep;
        if (h < 0.47f) return Lerp(shallow, beach, (h - 0.38f) / (0.09f));
        if (h < 0.63f) return Lerp(beach, land, (h - 0.47f) / (0.16f));
        if (h < 0.8f) return Lerp(land, mountain, (h - 0.63f) / (0.17f));
        return Lerp(mountain, snow, (h - 0.8f) / 0.2f);
    }

    static Rgba32 DesertRamp(float h)
    {
        var deep = new Rgba32(100, 60, 20);
        var sand = new Rgba32(230, 200, 140);
        var rock = new Rgba32(150, 110, 90);
        var dune = new Rgba32(210, 160, 100);

        if (h < 0.5f) return Lerp(deep, sand, h * 2f);
        return Lerp(dune, rock, (h - 0.5f) * 2f);
    }

    static Rgba32 IceRamp(float h)
    {
        var deep = new Rgba32(150, 190, 220);
        var ice = new Rgba32(220, 230, 240);
        var snow = new Rgba32(255, 255, 255);
        if (h < 0.4f) return deep;
        if (h < 0.8f) return ice;
        return snow;
    }

    static Rgba32 GasGiantRamp(float h)
    {
        // Use banded colors typical of gas giants (orange/tan/white/brown)
        var c1 = new Rgba32(220, 180, 120);
        var c2 = new Rgba32(200, 120, 70);
        var c3 = new Rgba32(240, 230, 200);

        if (h < 0.33f) return Lerp(c2, c1, h / 0.33f);
        if (h < 0.66f) return Lerp(c1, c3, (h - 0.33f) / 0.33f);
        return Lerp(c3, c2, (h - 0.66f) / 0.34f);
    }

    static Rgba32 LavaRamp(float h)
    {
        var black = new Rgba32(20, 16, 12);
        var lava = new Rgba32(255, 90, 0);
        var yellow = new Rgba32(255, 200, 80);
        return Lerp(black, Lerp(lava, yellow, MathF.Pow(h, 0.5f)), h);
    }

    static Rgba32 OceanRamp(float h)
    {
        var deep = new Rgba32(0, 30, 120);
        var surface = new Rgba32(30, 120, 200);
        return Lerp(deep, surface, h);
    }

    static Rgba32 RockyRamp(float h)
    {
        var dark = new Rgba32(80, 70, 60);
        var mid = new Rgba32(140, 130, 120);
        var bright = new Rgba32(200, 200, 200);
        if (h < 0.5f) return Lerp(dark, mid, h * 2f);
        return Lerp(mid, bright, (h - 0.5f) * 2f);
    }

    // --- NEW COLOR RAMPS ---

    static Rgba32 MetallicRamp(float h)
    {
        var dark = new Rgba32(60, 65, 70);
        var mid = new Rgba32(150, 155, 160);
        var bright = new Rgba32(210, 215, 220);
        if (h < 0.5f) return Lerp(dark, mid, h * 2f);
        return Lerp(mid, bright, (h - 0.5f) * 2f);
    }

    static Rgba32 IronRichRamp(float h)
    {
        var dark = new Rgba32(90, 50, 40);
        var mid = new Rgba32(180, 70, 50);
        var bright = new Rgba32(220, 150, 100);
        if (h < 0.5f) return Lerp(dark, mid, h * 2f);
        return Lerp(mid, bright, (h - 0.5f) * 2f);
    }

    static Rgba32 CarbonRichRamp(float h)
    {
        var black = new Rgba32(10, 10, 12);
        var dark_grey = new Rgba32(40, 40, 45);
        var light_grey = new Rgba32(90, 90, 90);
        if (h < 0.6f) return Lerp(black, dark_grey, h / 0.6f);
        return Lerp(dark_grey, light_grey, (h - 0.6f) / 0.4f);
    }

    static Rgba32 VolcanicRamp(float h)
    {
        var cool_rock = new Rgba32(30, 25, 35);
        var warm_rock = new Rgba32(80, 40, 50);
        var lava_glow = new Rgba32(255, 100, 20);
        if (h < 0.7f) return Lerp(cool_rock, warm_rock, h / 0.7f);
        return Lerp(warm_rock, lava_glow, (h - 0.7f) / 0.3f);
    }

    static Rgba32 TectonicRamp(float h)
    {
        var dark_strata = new Rgba32(90, 80, 70);
        var mid_strata = new Rgba32(140, 120, 110);
        var light_strata = new Rgba32(190, 185, 180);
        var peak = new Rgba32(220, 220, 215);
        if (h < 0.4f) return Lerp(dark_strata, mid_strata, h / 0.4f);
        if (h < 0.8f) return Lerp(mid_strata, light_strata, (h - 0.4f) / 0.4f);
        return Lerp(light_strata, peak, (h - 0.8f) / 0.2f);
    }

    static Rgba32 ChlorophyllRichRamp(float h)
    {
        var deep_green = new Rgba32(10, 60, 30);
        var forest = new Rgba32(20, 120, 60);
        var plains = new Rgba32(100, 160, 80);
        var high_plains = new Rgba32(180, 200, 120);
        if (h < 0.5f) return deep_green; // Shallow water / deep vegetation
        if (h < 0.7f) return Lerp(forest, plains, (h - 0.5f) / 0.2f);
        return Lerp(plains, high_plains, (h - 0.7f) / 0.3f);
    }

    static Rgba32 CometRamp(float h)
    {
        var dark_ice = new Rgba32(40, 45, 55);
        var dirty_snow = new Rgba32(150, 150, 160);
        var bright_ice = new Rgba32(220, 225, 235);
        if (h < 0.6f) return Lerp(dark_ice, dirty_snow, h / 0.6f);
        return Lerp(dirty_snow, bright_ice, (h - 0.6f) / 0.4f);
    }


    // ----------------- Main entry -----------------
    static void Main(string[] args)
    {
        // Parse args: first optional arg = number of random textures to generate.
        int count = 50;
        string outDir = "planet_textures_out";
        int w = DEFAULT_WIDTH, h = DEFAULT_HEIGHT;

        if (args.Length >= 1 && int.TryParse(args[0], out var parsed)) count = Math.Max(1, parsed);
        if (args.Length >= 2) outDir = args[1];
        if (args.Length >= 4)
        {
            if (int.TryParse(args[2], out var pw)) w = pw;
            if (int.TryParse(args[3], out var ph)) h = ph;
        }

        Console.WriteLine($"Generating {count} planet textures into '{outDir}' ({w}x{h})");
        GenerateRandomTextures(count, outDir, w, h);
        Console.WriteLine("Done.");
    }
}
