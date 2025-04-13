using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog; // Assuming you continue using NLog

namespace StellaLearning.Util;

/// <summary>
/// Manages the saving and loading of application data to persistent storage.
/// Provides functionality for registering savable data types, synchronous and asynchronous 
/// save/load operations, and automatic saving at specified intervals.
/// </summary>
public sealed class SaveDataManager : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<SaveDataManager> _instance = new(() => new SaveDataManager());

    /// <summary>
    /// Gets the singleton instance of the SaveDataManager.
    /// </summary>
    public static SaveDataManager Instance => _instance.Value;

    private readonly Dictionary<string, SavableDataInfo> _registeredData = [];
    private readonly string _saveDirectory;
    private Timer? _autoSaveTimer;
    private bool _isDisposed;

    // Default JSON options (can be overridden during registration)
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true, // For readability
        // Add any globally applicable converters here if needed
        // Converters = { ... }
        IncludeFields = true // Useful if some data classes use fields
    };

    private SaveDataManager()
    {
        // Determine base save directory (e.g., AppData)
        // Using AppData is generally better than CurrentDirectory for installed apps
        _saveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StellaLearning");

        // Or use CurrentDirectory if preferred, similar to your examples:
        // _saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "save");

        Directory.CreateDirectory(_saveDirectory); // Ensure base directory exists
        Logger.Info($"SaveDataManager initialized. Save directory: {_saveDirectory}");
    }

    /// <summary>
    /// Represents metadata for a piece of data managed by SaveDataManager.
    /// </summary>
    private class SavableDataInfo
    {
        public string Key { get; }
        public Type DataType { get; }
        public string FilePath { get; }
        public Func<object> DataGetter { get; } // Stores the Func returning object
        public JsonSerializerOptions SerializerOptions { get; }
        public SemaphoreSlim LockSemaphore { get; } = new SemaphoreSlim(1, 1); // Lock per file

        public SavableDataInfo(string key, Type dataType, string filePath, Func<object> dataGetter, JsonSerializerOptions options)
        {
            Key = key;
            DataType = dataType;
            FilePath = filePath;
            DataGetter = dataGetter;
            SerializerOptions = options;
        }
    }

    /// <summary>
    /// Registers a data type to be managed by the SaveDataManager.
    /// </summary>
    /// <typeparam name="T">The type of data to manage.</typeparam>
    /// <param name="key">A unique key identifying this data (e.g., "settings", "literature").</param>
    /// <param name="dataGetter">A function that returns the current instance of the data to be saved.</param>
    /// <param name="fileName">The filename (without path) to use for saving this data.</param>
    /// <param name="options">Optional custom JsonSerializerOptions for this specific data type.</param>
    public void RegisterSavable<T>(string key, Func<T> dataGetter, string fileName, JsonSerializerOptions? options = null) where T : class // Ensure T is a reference type for null checks
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        if (dataGetter == null) throw new ArgumentNullException(nameof(dataGetter));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

        if (_registeredData.ContainsKey(key))
        {
            Logger.Warn($"Data with key '{key}' already registered. Overwriting registration.");
        }

        string fullPath = Path.Combine(_saveDirectory, fileName);
        JsonSerializerOptions finalOptions = options ?? DefaultJsonOptions;

        // Wrap the specific Func<T> into a Func<object> for storage
        object getterWrapper() => dataGetter()!; // Use null-forgiving operator if T is class

        var info = new SavableDataInfo(key, typeof(T), fullPath, getterWrapper, finalOptions);
        _registeredData[key] = info;

        Logger.Info($"Registered savable data: Key='{key}', Type='{typeof(T).Name}', File='{fileName}'");
    }

    /// <summary>
    /// Asynchronously saves the specified data associated with the given key.
    /// </summary>
    /// <typeparam name="T">The type of data being saved.</typeparam>
    /// <param name="key">The unique key of the data to save.</param>
    /// <param name="data">The data object to save.</param>
    /// <returns>True if successful, False otherwise.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    private async Task<bool> SaveAsync<T>(string key, T data) where T : class
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_registeredData.TryGetValue(key, out SavableDataInfo? info))
        {
            Logger.Error($"Attempted to save data with unregistered key: '{key}'");
            return false;
        }

        if (info.DataType != typeof(T))
        {
            Logger.Error($"Type mismatch for key '{key}'. Expected '{info.DataType.Name}', got '{typeof(T).Name}'.");
            return false;
        }

        // Use the internal save helper
        return await InternalSaveAsync(info, data);
    }

    /// <summary>
    /// Asynchronously saves the current state of the data associated with the given key,
    /// using the registered dataGetter function.
    /// </summary>
    /// <param name="key">The unique key of the data to save.</param>
    /// <returns>True if successful, False otherwise.</returns>
    private async Task<bool> SaveCurrentAsync(string key)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (!_registeredData.TryGetValue(key, out SavableDataInfo? info))
        {
            Logger.Error($"Attempted to save data with unregistered key: '{key}'");
            return false;
        }

        try
        {
            object? data = info.DataGetter();
            if (data == null)
            {
                Logger.Warn($"DataGetter for key '{key}' returned null. Skipping save.");
                // Decide if null should be saved or skipped. Skipping is safer.
                // If you want to save null, remove this check and ensure InternalSaveAsync handles it.
                return true; // Or false depending on desired behavior for null data
            }
            return await InternalSaveAsync(info, data);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error invoking DataGetter for key '{key}' during SaveCurrentAsync.");
            return false;
        }
    }

    /// <summary>
    /// Internal helper to perform the actual saving logic with file locking.
    /// </summary>
    private static async Task<bool> InternalSaveAsync(SavableDataInfo info, object data)
    {
        bool success = false;
        // Wait to acquire the semaphore specific to this file path
        await info.LockSemaphore.WaitAsync();
        try
        {
            Logger.Debug($"Attempting to save data for key '{info.Key}' to '{info.FilePath}'");

            // Ensure the directory exists (might be redundant but safe)
            string? directory = Path.GetDirectoryName(info.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            else
            {
                Logger.Error($"Could not determine directory for path: {info.FilePath}");
                return false; // Cannot proceed without a directory
            }

            // Serialize and write asynchronously
            // Use FileStream for async writing, overwriting existing file
            await using FileStream createStream = new FileStream(info.FilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(createStream, data, info.DataType, info.SerializerOptions);

            success = true;
            Logger.Info($"Successfully saved data for key '{info.Key}' to '{info.FilePath}'");
        }
        catch (JsonException jsonEx)
        {
            Logger.Error(jsonEx, $"JSON serialization error saving data for key '{info.Key}' to '{info.FilePath}'.");
        }
        catch (IOException ioEx)
        {
            Logger.Error(ioEx, $"IO error saving data for key '{info.Key}' to '{info.FilePath}'. Check permissions or if file is locked.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Unexpected error saving data for key '{info.Key}' to '{info.FilePath}'.");
        }
        finally
        {
            // Release the semaphore
            info.LockSemaphore.Release();
        }
        return success;
    }

    /// <summary>
    /// Asynchronously loads the data associated with the given key.
    /// </summary>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <param name="key">The unique key of the data to load.</param>
    /// <returns>The loaded data object, or default(T) (e.g., null) if loading fails or the file doesn't exist.</returns>
    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (!_registeredData.TryGetValue(key, out SavableDataInfo? info))
        {
            Logger.Error($"Attempted to load data with unregistered key: '{key}'");
            return default; // Or throw? Default is safer.
        }

        if (info.DataType != typeof(T))
        {
            Logger.Error($"Type mismatch for key '{key}'. Expected '{info.DataType.Name}', loading as '{typeof(T).Name}'. Returning default.");
            return default;
        }

        // Wait to acquire the semaphore specific to this file path
        await info.LockSemaphore.WaitAsync();
        try
        {
            if (!File.Exists(info.FilePath))
            {
                Logger.Info($"Save file for key '{info.Key}' not found at '{info.FilePath}'. Returning default.");
                return default;
            }

            Logger.Debug($"Attempting to load data for key '{info.Key}' from '{info.FilePath}'");

            // Read and deserialize asynchronously
            await using FileStream openStream = new FileStream(info.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var loadedData = await JsonSerializer.DeserializeAsync(openStream, info.DataType, info.SerializerOptions);

            if (loadedData is T typedData)
            {
                Logger.Info($"Successfully loaded data for key '{info.Key}' from '{info.FilePath}'.");
                return typedData;
            }
            else
            {
                // This should ideally not happen if info.DataType matches typeof(T)
                // and deserialization succeeds, but handle defensively.
                Logger.Error($"Deserialized data for key '{info.Key}' is not of expected type '{typeof(T).Name}'. Actual type: '{loadedData?.GetType().Name ?? "null"}'.");
                return default;
            }
        }
        catch (JsonException jsonEx)
        {
            Logger.Error(jsonEx, $"JSON deserialization error loading data for key '{info.Key}' from '{info.FilePath}'. File might be corrupted.");
            // TODO: Consider backup/recovery strategies here
            return default;
        }
        catch (IOException ioEx)
        {
            Logger.Error(ioEx, $"IO error loading data for key '{info.Key}' from '{info.FilePath}'. Check permissions or if file exists.");
            return default;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Unexpected error loading data for key '{info.Key}' from '{info.FilePath}'.");
            return default;
        }
        finally
        {
            // Release the semaphore
            info.LockSemaphore.Release();
        }
    }

    /// <summary>
    /// Asynchronously saves the current state of ALL registered data items sequentially.
    /// Uses the registered dataGetter functions.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation. Returns true if all saves were successful or skipped (due to null data), false otherwise.</returns>
    public async Task<bool> SaveAllAsync()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        Logger.Info($"Starting sequential SaveAllAsync for {_registeredData.Count} items.");

        bool allSucceeded = true;
        int successCount = 0;
        int skippedCount = 0;
        int failCount = 0;

        // Run saves sequentially
        foreach (var kvp in _registeredData)
        {
            SavableDataInfo info = kvp.Value;
            object? data = info.DataGetter();

            bool itemResult = false; // Assume failure until success or skip
            try
            {
                if (data != null)
                {
                    // Await each save individually
                    itemResult = await InternalSaveAsync(info, data);
                }
                else
                {
                    Logger.Warn($"DataGetter for key '{info.Key}' returned null during SaveAllAsync. Skipping save for this item.");
                    itemResult = true; // Treat skipped as "not failed" for overall success tracking
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                // Catch errors from DataGetter or unexpected errors during the await InternalSaveAsync chain
                Logger.Error(ex, $"Error invoking DataGetter or processing save for key '{info.Key}' during SaveAllAsync.");
                itemResult = false; // Ensure failure is recorded
            }

            if (itemResult)
            {
                // Only count explicit successes, not skips
                if (data != null) successCount++;
            }
            else
            {
                failCount++;
                allSucceeded = false; // Mark overall failure if any item fails
            }
        } // End foreach loop

        int totalProcessed = successCount + skippedCount + failCount;

        if (failCount > 0)
        {
            Logger.Warn($"SaveAllAsync completed with {failCount} failures, {successCount} successes, {skippedCount} skipped out of {totalProcessed} items.");
        }
        else
        {
            Logger.Info($"SaveAllAsync completed successfully for {successCount} items ({skippedCount} skipped) out of {totalProcessed}.");
        }

        return allSucceeded;
    }

    /// <summary>
    /// Starts the auto-save timer.
    /// </summary>
    /// <param name="interval">The interval between automatic saves.</param>
    public void StartAutoSave(TimeSpan interval)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), "Auto-save interval must be positive.");

        // Stop existing timer if any
        StopAutoSave();

        Logger.Info($"Starting auto-save timer with interval: {interval}");
        _autoSaveTimer = new Timer(
            async _ => await AutoSaveCallback(), // Timer callback
            null,       // State object (not needed here)
            interval,   // Time to wait before first execution
            interval);  // Interval between subsequent executions
    }

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    public void StopAutoSave()
    {
        if (_isDisposed) return; // Don't throw if already disposed

        if (_autoSaveTimer != null)
        {
            Logger.Info("Stopping auto-save timer.");
            _autoSaveTimer.Dispose();
            _autoSaveTimer = null;
        }
    }

    private async Task AutoSaveCallback()
    {
        // Prevent re-entrancy if SaveAllAsync takes longer than the interval?
        // A simple approach is just to log and proceed. More complex might use a flag.
        Logger.Debug("Auto-save triggered.");
        try
        {
            await SaveAllAsync();
        }
        catch (Exception ex)
        {
            // Catch exceptions from SaveAllAsync itself if it throws
            Logger.Error(ex, "Exception occurred during auto-save execution.");
        }
    }


    /// <summary>
    /// Disposes the SaveDataManager, stopping the auto-save timer and releasing semaphores.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        Logger.Info("Disposing SaveDataManager...");

        // Stop and dispose the timer
        StopAutoSave();

        // Dispose semaphores
        foreach (var info in _registeredData.Values)
        {
            info.LockSemaphore.Dispose();
        }
        _registeredData.Clear(); // Clear the registrations

        Logger.Info("SaveDataManager disposed.");
        // Suppress finalization because we've cleaned up.
        GC.SuppressFinalize(this);
    }

    // Optional: Finalizer as a safeguard, although explicit Dispose is preferred.
    // ~SaveDataManager()
    // {
    //     if (!_isDisposed)
    //     {
    //          Logger.Warn("SaveDataManager finalized without being explicitly disposed. Potential resource leak.");
    //          Dispose(); // Call Dispose from finalizer (careful about managed objects)
    //     }
    // }
}