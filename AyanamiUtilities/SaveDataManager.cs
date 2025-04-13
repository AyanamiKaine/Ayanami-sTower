using System.Diagnostics;
using System.Text.Json;

namespace Ayanami.Utilities;

/// <summary>
/// Defines a contract for objects that can save their state to a stream
/// and load their state from a stream.
/// The implementing class is responsible for choosing the serialization format (e.g., JSON, XML, Binary).
/// </summary>
public interface ISaveLoadable
{
    /// <summary>
    /// Saves the current state of the object to the specified stream.
    /// The stream should remain open after the operation for the caller to manage.
    /// </summary>
    /// <param name="stream">The stream to write the object's state to. Must be writable.</param>
    /// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the stream is not writable.</exception>
    /// <exception cref="Exception">Thrown if any serialization or I/O error occurs during saving.</exception>
    void Save(Stream stream);

    /// <summary>
    /// Loads the state of the object from the specified stream.
    /// This typically overwrites the current state of the object instance on which this method is called.
    /// The stream should remain open after the operation for the caller to manage.
    /// </summary>
    /// <param name="stream">The stream to read the object's state from. Must be readable.</param>
    /// <exception cref="ArgumentNullException">Thrown if the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the stream is not readable.</exception>
    /// <exception cref="Exception">Thrown if any deserialization or I/O error occurs during loading (e.g., format mismatch, corrupted data).</exception>
    void Load(Stream stream);
}

/// <summary>
/// This savedata manager should make it trivially to save anyform of data easily to json and load it.
/// It has an autosafe function and only saves data when it actually changed. I uses System.Threading.Timer
/// under the hood, so the DataSetter action will run on another thread.
/// </summary>
public class SaveDataManager
{
    private readonly Timer _timer;
    private bool _timerRunning = false;

    /// <summary>
    /// Where the save data gets stored
    /// </summary>
    public required string SaveDirectory { get; set; }
    private readonly Dictionary<string, ISaveLoadable> _registeredData = [];

    /// <summary>
    /// Creates a save data manager instance.
    /// </summary>
    /// <param name="interval">The interval between automatic saves.</param>
    /// <param name="applicationName">Name used for the root save data folder</param>
    public SaveDataManager(TimeSpan interval, string applicationName)
    {
        SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            applicationName);
        Directory.CreateDirectory(SaveDirectory);

        Debug.WriteLine($"SaveDataManager initialized. Save directory: {SaveDirectory}");

        _timer = new(SaveAllData, null, interval, interval);
    }

    /// <summary>
    /// Represents metadata for a piece of data managed by SaveDataManager.
    /// </summary>
    private class SavableDataInfo
    {
        /// <summary>
        /// Used to identify the save data, used to define the name of the saved file.
        /// Its also possible to say /config/user.json. So the save manager stores
        /// the file nested.
        /// </summary>
        public string Key { get; }
        public JsonSerializerOptions SerializerOptions { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">
        /// Used to identify the save data, used to define the name of the saved file.
        /// Its also possible to say /config/user.json. So the save manager stores
        /// the file nested.
        /// </param>
        public SavableDataInfo(string key)
        {
            Key = key;

            // Sane defaults
            SerializerOptions = new()
            {
                WriteIndented = true,
                IncludeFields = true
            };
        }

        public SavableDataInfo(string key, JsonSerializerOptions options)
        {
            Key = key;
            SerializerOptions = options;
        }
    }

    /// <summary>
    /// Saves all data registered.
    /// </summary>
    /// <param name="state"></param>
    private void SaveAllData(object? state)
    {

    }

    private void LoadAllData(object? state)
    {

    }
}