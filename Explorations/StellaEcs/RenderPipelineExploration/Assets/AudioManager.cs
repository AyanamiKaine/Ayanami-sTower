using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonWorks;
using MoonWorks.Audio;

namespace AyanamisTower.StellaEcs.StellaInvicta.Assets;

/// <summary>
/// Simple, high-level audio API on top of MoonWorks.
/// - LoadClip: caches decoded audio (WAV/OGG) as AudioBuffer
/// - PlayOneShot: fire-and-forget SFX
/// - CreateInstance: controllable instance (play/pause/stop/volume/pan/pitch)
/// - PlayMusic: streaming OGG music with loop controls
/// Mirrors AssetManager pattern: methods take Game and TitleStorage-relative paths.
/// </summary>
public static class AudioManager
{
    // Cache of fully-decoded clips (AudioBuffer). Key is normalized TitleStorage path
    private static readonly ConcurrentDictionary<string, AudioBuffer> s_clipCache = new();

    // Single music stream handle (one at a time for simplicity)
    private static MusicHandle? s_music;

    /// <summary>
    /// Normalize a path to a TitleStorage-friendly relative POSIX path.
    /// </summary>
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
        }
        return normalized.TrimStart('/');
    }

    /// <summary>
    /// Load and cache a decoded clip from WAV or OGG. Returns null on failure.
    /// </summary>
    public static AudioBuffer? LoadClip(Game game, string path)
    {
        var key = NormalizeTitlePath(path);
        if (s_clipCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Quick existence check
        if (!game.RootTitleStorage.GetFileSize(key, out _))
        {
            Console.WriteLine($"[AudioManager] File not found: {key}");
            return null;
        }

        AudioBuffer? buffer = null;
        var ext = Path.GetExtension(key).ToLowerInvariant();
        try
        {
            if (ext == ".wav")
            {
                buffer = AudioDataWav.CreateBuffer(game.AudioDevice, game.RootTitleStorage, key);
            }
            else if (ext == ".ogg")
            {
                // Decode entire OGG into an AudioBuffer for SFX
                buffer = AudioDataOgg.CreateBuffer(game.AudioDevice, game.RootTitleStorage, key);
            }
            else
            {
                Console.WriteLine($"[AudioManager] Unsupported audio extension: {ext} ({key}). Supported formats are .wav and .ogg");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioManager] LoadClip error for {key}: {ex.Message}");
            buffer = null;
        }

        if (buffer != null)
        {
            s_clipCache.TryAdd(key, buffer);
        }

        return buffer;
    }

    /// <summary>
    /// Fire-and-forget one-shot playback. Good for SFX. Returns true if started.
    /// </summary>
    public static bool PlayOneShot(Game game, string path, float volume = 1f, float pan = 0f, float pitch = 0f)
    {
        var clip = LoadClip(game, path);
        if (clip == null) { return false; }

        var voice = game.AudioDevice.Obtain<TransientVoice>(clip.Format);
        voice.Submit(clip);
        voice.SetVolume(volume);
        voice.SetPan(pan);
        voice.SetPitch(pitch);
        voice.Play();
        return true;
    }

    /// <summary>
    /// Create a controllable instance. Uses a PersistentVoice under the hood.
    /// </summary>
    public static SoundInstance? CreateInstance(Game game, string path, bool loop = false)
    {
        var clip = LoadClip(game, path);
        if (clip == null) { return null; }

        var voice = PersistentVoice.Create(game.AudioDevice, clip.Format);
        voice.Submit(clip, loop);
        return new SoundInstance(voice);
    }

    /// <summary>
    /// Start streaming music from an OGG file. Stops any currently playing music.
    /// </summary>
    public static bool PlayMusic(Game game, string path, bool loop = true, float volume = 1f)
    {
        StopMusic();

        var key = NormalizeTitlePath(path);
        if (!game.RootTitleStorage.GetFileSize(key, out var size) || size == 0)
        {
            Console.WriteLine($"[AudioManager] Music file not found or empty: {key}");
            return false;
        }

        var bytes = new byte[size];
        if (!game.RootTitleStorage.ReadFile(key, bytes))
        {
            Console.WriteLine($"[AudioManager] Failed to read music: {key}");
            return false;
        }

        try
        {
            var data = AudioDataOgg.Create(game.AudioDevice);
            data.Open(bytes);
            data.Loop = loop;

            var voice = game.AudioDevice.Obtain<PersistentVoice>(data.Format);
            voice.SetVolume(volume);
            data.SendTo(voice);
            voice.Play();

            s_music = new MusicHandle(data, voice);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioManager] PlayMusic error for {key}: {ex.Message}");
            return false;
        }
    }
    /// <summary>
    /// Stops the currently playing music stream and releases its resources.
    /// Safe to call even if no music is playing.
    /// </summary>
    public static void StopMusic()
    {
        if (s_music != null)
        {
            s_music.Dispose();
            s_music = null;
        }
    }
    /// <summary>
    /// Pauses the currently playing music stream.
    /// </summary>
    public static void PauseMusic()
    {
        if (s_music?.Voice != null)
        {
            s_music.Voice.Pause();
        }
    }
    /// <summary>
    /// Resumes the currently paused music stream.
    /// </summary>
    public static void ResumeMusic()
    {
        if (s_music?.Voice != null)
        {
            s_music.Voice.Play();
        }
    }
    /// <summary>
    /// Sets the music volume [0..]. Values are clamped internally by MoonWorks.
    /// </summary>
    /// <param name="volume">New music volume (0 = silent).</param>
    public static void SetMusicVolume(float volume)
    {
        if (s_music?.Voice != null)
        {
            s_music.Voice.SetVolume(volume);
        }
    }

    /// <summary>
    /// Remove a clip from cache and return it for disposal, or null if not cached.
    /// </summary>
    public static AudioBuffer? RemoveClip(string path)
    {
        var key = NormalizeTitlePath(path);
        if (s_clipCache.TryRemove(key, out var buf))
        {
            return buf;
        }
        return null;
    }

    /// <summary>
    /// Clear all cached clips and return them so the caller can Dispose if desired.
    /// </summary>
    public static IEnumerable<AudioBuffer> ClearClipCache()
    {
        var values = s_clipCache.Values.ToArray();
        s_clipCache.Clear();
        return values;
    }

    // ---------------------
    // Handle/helper classes
    // ---------------------

    /// <summary>
    /// Controllable sound instance backed by a PersistentVoice.
    /// </summary>
    public sealed class SoundInstance : IDisposable
    {
        /// <summary>
        /// Underlying voice for advanced control if needed.
        /// </summary>
        public PersistentVoice Voice { get; }
        private bool _disposed;

        internal SoundInstance(PersistentVoice voice)
        {
            Voice = voice;
        }

        /// <summary>
        /// Begins or resumes playback.
        /// </summary>
        public void Play() => Voice.Play();
        /// <summary>
        /// Pauses playback, preserving queued buffers and cursor position.
        /// </summary>
        public void Pause() => Voice.Pause();
        /// <summary>
        /// Stops playback and flushes queued buffers.
        /// </summary>
        public void Stop() => Voice.Stop();

        /// <summary>
        /// Sets the instance volume (0 = silent).
        /// </summary>
        public void SetVolume(float v) => Voice.SetVolume(v);
        /// <summary>
        /// Sets left/right pan (-1 = hard left, +1 = hard right).
        /// </summary>
        public void SetPan(float p) => Voice.SetPan(p);
        /// <summary>
        /// Sets pitch in semitone-like range (-1..+1), mapped by MoonWorks.
        /// </summary>
        public void SetPitch(float p) => Voice.SetPitch(p);

        /// <summary>
        /// Stops playback and returns the underlying voice to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                Voice.Stop();
                Voice.Return(); // give the voice back to the pool
            }
            catch { /* no-throw */ }
            _disposed = true;
        }
    }

    /// <summary>
    /// Streaming music handle: owns AudioDataOgg and a PersistentVoice.
    /// </summary>
    private sealed class MusicHandle : IDisposable
    {
        public AudioDataOgg Data { get; }
        public PersistentVoice Voice { get; }
        private bool _disposed;

        public MusicHandle(AudioDataOgg data, PersistentVoice voice)
        {
            Data = data;
            Voice = voice;
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                Data.Disconnect();
                Data.Close();
                Voice.Stop();
                Voice.Return();
            }
            catch { /* swallow */ }
            _disposed = true;
        }
    }
}
