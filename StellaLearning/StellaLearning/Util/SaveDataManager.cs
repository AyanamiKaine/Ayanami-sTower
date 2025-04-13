using System;
using System.Collections.Generic;
using System.Diagnostics; // For StackTrace potentially, or just rely on logger context
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace StellaLearning.Util;

/// <summary>
/// Manages the saving and loading of application data to persistent storage.
/// Provides functionality for registering savable data types with getters and non-null setters (using a No-Op default),
/// asynchronous save/load operations, and automatic saving at specified intervals.
/// </summary>
public sealed class SaveDataManager : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<SaveDataManager> _instance = new(() => new SaveDataManager());

    /// <summary>
    /// Gets the singleton instance of the SaveDataManager.
    /// </summary>
    public static SaveDataManager Instance => _instance.Value;

    /// <summary>
    /// A default Action object that performs no operation but logs its invocation at Trace level.
    /// Used when no specific DataSetter is provided during registration.
    /// </summary>
    private static readonly Action<object> NoOpDataSetter = (obj) =>
    {
        // This action is static, so it doesn't inherently know the 'key'.
        // The context (key) should be logged just before this is invoked in LoadAsync.
        Logger.Trace($"Invoked NoOpDataSetter. No user-defined action executed for object type '{obj?.GetType().Name ?? "null"}'.");
    };

    private readonly Dictionary<string, SavableDataInfo> _registeredData = [];
    private readonly string _saveDirectory;
    private Timer? _autoSaveTimer;
    private bool _isDisposed;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    private SaveDataManager()
    {
        _saveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StellaLearning");
        Directory.CreateDirectory(_saveDirectory);
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
        public Func<object> DataGetter { get; }
        public Action<object> DataSetter { get; } // << NO LONGER Nullable
        public JsonSerializerOptions SerializerOptions { get; }
        public SemaphoreSlim LockSemaphore { get; } = new SemaphoreSlim(1, 1);

        // Constructor updated to expect non-nullable Action<object>
        public SavableDataInfo(string key, Type dataType, string filePath, Func<object> dataGetter, Action<object> dataSetter, JsonSerializerOptions options)
        {
            Key = key;
            DataType = dataType;
            FilePath = filePath;
            DataGetter = dataGetter;
            DataSetter = dataSetter; // << Assignment of non-nullable
            SerializerOptions = options;
        }
    }

    /// <summary>
    /// Registers a data type to be managed by the SaveDataManager.
    /// </summary>
    /// <typeparam name="T">The type of data to manage.</typeparam>
    /// <param name="key">A unique key identifying this data.</param>
    /// <param name="dataGetter">A function that returns the current instance of the data to be saved.</param>
    /// <param name="dataSetter">An optional action that takes the loaded data instance and applies it back to the application's state. If null, a No-Op logger action is used.</param>
    /// <param name="fileName">The filename (without path) to use for saving this data.</param>
    /// <param name="options">Optional custom JsonSerializerOptions for this specific data type.</param>
    public void RegisterSavable<T>(
        string key,
        Func<T> dataGetter,
        Action<T>? dataSetter, // << Stays nullable for the public API
        string fileName,
        JsonSerializerOptions? options = null) where T : class
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
        if (dataGetter == null) throw new ArgumentNullException(nameof(dataGetter));
        // dataSetter can be null
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));

        if (_registeredData.ContainsKey(key))
        {
            Logger.Warn($"Data with key '{key}' already registered. Overwriting registration.");
            // Consider semaphore disposal/management if overwriting implies resource change.
        }

        string fullPath = Path.Combine(_saveDirectory, fileName);
        JsonSerializerOptions finalOptions = options ?? DefaultJsonOptions;

        object getterWrapper() => dataGetter()!;

        Action<object> setterWrapper; // No longer nullable
        if (dataSetter != null)
        {
            // Capture the user's delegate instance to prevent issues with loop variables if used later
            Action<T> capturedSetter = dataSetter;
            // Create the specific wrapper
            setterWrapper = (obj) =>
            {
                if (obj is T typedData)
                {
                    try
                    {
                        // Call the original user-provided setter
                        capturedSetter(typedData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Exception occurred within the registered DataSetter for key '{key}' when processing type '{typeof(T).Name}'.");
                        // The exception is logged, but LoadAsync will still return the data.
                    }
                }
                else
                {
                    Logger.Error($"DataSetter wrapper for key '{key}' received an incompatible object type. Expected '{typeof(T).Name}', got '{obj?.GetType().Name ?? "null"}'. This indicates an internal issue.");
                }
            };
            Logger.Trace($"Registered specific DataSetter for key '{key}'.");
        }
        else
        {
            // Assign the static No-Op action if user provided null
            setterWrapper = NoOpDataSetter;
            Logger.Trace($"Registered NoOpDataSetter for key '{key}'.");
        }

        // Pass the guaranteed non-nullable setterWrapper to the constructor
        var info = new SavableDataInfo(key, typeof(T), fullPath, getterWrapper, setterWrapper, finalOptions);
        _registeredData[key] = info;

        // Updated log message slightly
        Logger.Info($"Registered savable data: Key='{key}', Type='{typeof(T).Name}', File='{fileName}' (Setter Type: {(dataSetter != null ? "Specific" : "NoOp")})");
    }

    // ... (SaveAsync, SaveCurrentAsync, InternalSaveAsync methods remain the same) ...
    // ... (Make sure they are included in your final file) ...
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
        await info.LockSemaphore.WaitAsync();
        try
        {
            Logger.Debug($"Attempting to save data for key '{info.Key}' to '{info.FilePath}'");
            string? directory = Path.GetDirectoryName(info.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            else
            {
                Logger.Error($"Could not determine directory for path: {info.FilePath}");
                return false;
            }

            await using FileStream createStream = new FileStream(info.FilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(createStream, data, info.DataType, info.SerializerOptions);
            success = true;
            Logger.Info($"Successfully saved data for key '{info.Key}' to '{info.FilePath}'");
        }
        catch (JsonException jsonEx) { Logger.Error(jsonEx, $"JSON serialization error saving data for key '{info.Key}' to '{info.FilePath}'."); }
        catch (IOException ioEx) { Logger.Error(ioEx, $"IO error saving data for key '{info.Key}' to '{info.FilePath}'."); }
        catch (Exception ex) { Logger.Error(ex, $"Unexpected error saving data for key '{info.Key}' to '{info.FilePath}'."); }
        finally { info.LockSemaphore.Release(); }
        return success;
    }

    /// <summary>
    /// Asynchronously loads the data associated with the given key.
    /// The registered `DataSetter` (either specific or No-Op) will be invoked with the loaded data
    /// before this method returns, provided loading and type casting are successful.
    /// </summary>
    /// <typeparam name="T">The expected type of the data.</typeparam>
    /// <param name="key">The unique key of the data to load.</param>
    /// <returns>The loaded data object, or default(T) if loading fails, the file doesn't exist, or a type mismatch occurs.</returns>
    public async Task<T?> LoadAsync<T>(string key) where T : class // << XML Comment Updated
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SaveDataManager));
        if (!_registeredData.TryGetValue(key, out SavableDataInfo? info))
        {
            Logger.Error($"Attempted to load data with unregistered key: '{key}'");
            return default;
        }

        if (info.DataType != typeof(T))
        {
            Logger.Error($"Type mismatch for key '{key}'. Registered type is '{info.DataType.Name}', loading as '{typeof(T).Name}'. Returning default.");
            return default;
        }

        await info.LockSemaphore.WaitAsync();
        T? resultData = default;
        try
        {
            if (!File.Exists(info.FilePath))
            {
                Logger.Info($"Save file for key '{info.Key}' not found at '{info.FilePath}'. Returning default.");
                return default; // Keep returning default here
            }

            Logger.Debug($"Attempting to load data for key '{info.Key}' from '{info.FilePath}'");

            await using FileStream openStream = new FileStream(info.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            var loadedData = await JsonSerializer.DeserializeAsync(openStream, info.DataType, info.SerializerOptions);

            if (loadedData is T typedData)
            {
                Logger.Info($"Successfully loaded data for key '{info.Key}' from '{info.FilePath}'.");

                // --- UPDATED: Directly invoke the DataSetter ---
                // No null check needed here, as info.DataSetter is guaranteed non-null.
                Logger.Debug($"Invoking DataSetter for key '{info.Key}'...");
                info.DataSetter(typedData); // Will call specific wrapper or NoOpDataSetter
                // The NoOpDataSetter logs its own message at Trace level if it runs.
                // The specific wrapper logs errors internally if the user code throws.
                Logger.Debug($"DataSetter for key '{info.Key}' invocation completed.");
                // --- End UPDATED ---

                resultData = typedData;
            }
            else if (loadedData == null && File.Exists(info.FilePath))
            {
                Logger.Info($"Successfully loaded 'null' data for key '{info.Key}' from '{info.FilePath}'. Setter not invoked for null data.");
                // We explicitly do *not* call the setter here, even the NoOp one,
                // as there's no valid object instance to pass.
                resultData = default;
            }
            else
            {
                Logger.Error($"Deserialized data for key '{info.Key}' is not assignable to the expected type '{typeof(T).Name}'. Actual deserialized type: '{loadedData?.GetType().Name ?? "null"}'. Returning default. Setter not invoked.");
                resultData = default;
            }
        }
        catch (JsonException jsonEx)
        {
            Logger.Error(jsonEx, $"JSON deserialization error loading data for key '{info.Key}' from '{info.FilePath}'. File might be corrupted/incompatible. Setter not invoked.");
            resultData = default;
        }
        catch (IOException ioEx)
        {
            Logger.Error(ioEx, $"IO error loading data for key '{info.Key}' from '{info.FilePath}'. Setter not invoked.");
            resultData = default;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Unexpected error loading or processing data for key '{info.Key}' from '{info.FilePath}'. Setter not invoked.");
            resultData = default;
        }
        finally
        {
            info.LockSemaphore.Release();
        }
        return resultData;
    }

    // ... (SaveAllAsync, StartAutoSave, StopAutoSave, AutoSaveCallback, Dispose methods remain the same) ...
    // ... (Make sure they are included in your final file) ...

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

        var keysToSave = _registeredData.Keys.ToList(); // Snapshot keys

        foreach (var key in keysToSave)
        {
            if (!_registeredData.TryGetValue(key, out SavableDataInfo? info))
            {
                Logger.Warn($"Key '{key}' disappeared during SaveAllAsync execution. Skipping.");
                failCount++; allSucceeded = false; continue;
            }

            bool itemResult = false; object? data = null;
            try
            {
                data = info.DataGetter();
                if (data != null) { itemResult = await InternalSaveAsync(info, data); }
                else { Logger.Warn($"DataGetter for key '{info.Key}' returned null during SaveAllAsync. Skipping save."); itemResult = true; skippedCount++; }
            }
            catch (Exception ex) { Logger.Error(ex, $"Error invoking DataGetter or processing save for key '{info.Key}' during SaveAllAsync."); itemResult = false; }

            if (itemResult) { if (data != null) successCount++; }
            else { failCount++; allSucceeded = false; }
        }

        int totalProcessed = successCount + skippedCount + failCount;
        string summary = $"SaveAllAsync completed. Success: {successCount}, Skipped (null): {skippedCount}, Failed: {failCount} out of {totalProcessed} processed (Target: {keysToSave.Count}).";
        if (failCount > 0) { Logger.Warn(summary); } else { Logger.Info(summary); }

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
        StopAutoSave(); // Stop existing timer if any
        Logger.Info($"Starting auto-save timer with interval: {interval}");
        _autoSaveTimer = new Timer(async state => await AutoSaveCallback(state), null, interval, interval);
    }

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    public void StopAutoSave()
    {
        if (_isDisposed) return;
        if (_autoSaveTimer != null)
        {
            Logger.Info("Stopping auto-save timer...");
            _autoSaveTimer.Dispose(); // Dispose waits for callback completion
            _autoSaveTimer = null;
            Logger.Info("Auto-save timer stopped.");
        }
    }

    private readonly object _autoSaveLock = new object();
    private async Task AutoSaveCallback(object? state)
    {
        if (!Monitor.TryEnter(_autoSaveLock)) { Logger.Warn("Auto-save callback skipped; previous save still running."); return; }
        try
        {
            if (_isDisposed) return; // Check again inside lock
            Logger.Debug("Auto-save triggered.");
            await SaveAllAsync(); // Log results are within SaveAllAsync
        }
        catch (ObjectDisposedException) { Logger.Debug("Auto-save callback aborted due to SaveDataManager disposal."); }
        catch (Exception ex) { Logger.Error(ex, "Unexpected exception during auto-save callback."); }
        finally { Monitor.Exit(_autoSaveLock); }
    }


    /// <summary>
    /// Disposes the SaveDataManager, stopping the auto-save timer and releasing semaphores.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            Logger.Info("Disposing SaveDataManager managed resources...");
            StopAutoSave();
            var infosToDispose = _registeredData.Values.ToList();
            _registeredData.Clear();
            foreach (var info in infosToDispose)
            {
                try { info.LockSemaphore.Dispose(); }
                catch (Exception ex) { Logger.Error(ex, $"Error disposing semaphore for key '{info.Key}'."); }
            }
            Logger.Info("SaveDataManager managed resources disposed.");
        }
        // Release unmanaged resources here if any
        _isDisposed = true;
    }
}