// StellaSharedMemory.cs
// This class provides a reusable, disposable abstraction for handling
// the complexities of cross-platform shared memory, including file-based locking.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaSharedMemory;

/// <summary>
/// Specifies whether to create a new shared memory segment or open an existing one.
/// </summary>
public enum SharedMemoryMode
{
    /// <summary>
    /// Creates a new shared memory segment.
    /// </summary>
    Create,

    /// <summary>
    /// Opens an existing shared memory segment.
    /// </summary>
    Open
}

/// <summary>
/// Manages a file-backed, cross-platform shared memory segment with locking.
/// Implements IDisposable to ensure that locks are released and resources are cleaned up.
/// </summary>
public class SharedMemory : IDisposable
{
    private readonly string _lockPath;
    private readonly FileStream _fileStream;
    private readonly MemoryMappedFile _memoryMappedFile;

    /// <summary>
    /// The stream that provides access to the shared memory.
    /// </summary>
    public MemoryMappedViewStream ViewStream { get; }

    /// <summary>
    /// Initializes a new instance of the StellaSharedMemory class.
    /// </summary>
    /// <param name="name">The unique name for the shared memory segment.</param>
    /// <param name="mode">Whether to create a new segment or open an existing one.</param>
    /// <param name="capacity">The size of the memory segment in bytes. Required when creating.</param>
    public SharedMemory(string name, SharedMemoryMode mode, long capacity = 0)
    {
        string mmfPath = Path.Combine(GetSharedDirectory(), name);
        _lockPath = Path.Combine(GetSharedDirectory(), name + ".lock");

        AcquireLock();

        try
        {
            if (mode == SharedMemoryMode.Create && capacity <= 0)
            {
                throw new ArgumentException("Capacity must be greater than zero when creating a new shared memory file.", nameof(capacity));
            }

            // If we are in 'Open' mode, we must wait for the file to exist first.
            if (mode == SharedMemoryMode.Open && !WaitForFile(mmfPath))
            {
                throw new FileNotFoundException("The shared memory file was not found.", mmfPath);
            }

            FileMode fileMode = mode == SharedMemoryMode.Create ? FileMode.Create : FileMode.Open;
            _fileStream = new FileStream(mmfPath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite);

            long mmfCapacity = mode == SharedMemoryMode.Create ? capacity : _fileStream.Length;
            _memoryMappedFile = MemoryMappedFile.CreateFromFile(_fileStream, null, mmfCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            ViewStream = _memoryMappedFile.CreateViewStream();
        }
        catch
        {
            // If any exception occurs during construction after the lock is acquired,
            // we must guarantee that the lock is released before propagating the exception.
            ReleaseLock();
            throw;
        }
    }

    /// <summary>
    /// Disposes of the managed and unmanaged resources.
    /// This will be called automatically at the end of a 'using' block.
    /// </summary>
    public void Dispose()
    {
        ViewStream?.Dispose();
        _memoryMappedFile?.Dispose();
        _fileStream?.Dispose();
        ReleaseLock();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to act as a safeguard in case Dispose() is not called.
    /// </summary>
    ~SharedMemory()
    {
        ReleaseLock();
    }

    private static string GetSharedDirectory()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/shm" : Path.GetTempPath();
    }

    private void AcquireLock()
    {
        while (true)
        {
            try
            {
                using (new FileStream(_lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    return; // Lock acquired
                }
            }
            catch (IOException)
            {
                Thread.Sleep(100); // Lock is held, wait and retry
            }
        }
    }

    private void ReleaseLock()
    {
        if (File.Exists(_lockPath))
        {
            File.Delete(_lockPath);
        }
    }

    private static bool WaitForFile(string path)
    {
        // Wait up to 10 seconds for the file to appear.
        for (int i = 0; i < 100; i++)
        {
            if (File.Exists(path)) return true;
            Thread.Sleep(100);
        }
        return false;
    }
}
