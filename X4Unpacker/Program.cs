using System.Security.Cryptography;

namespace X4Unpacker;

static class Program
{
    private const int BufferSize = 1024 * 1024;
    private static readonly Lock _consoleLock = new();

    static void Main(string[] args)
    {
        lock (_consoleLock)
        {
            Console.WriteLine("X4 Foundations .cat/.dat Unpacker (Strict Deduplication)");
            Console.WriteLine("------------------------------------------------------");
        }

        string inputPath = ".";
        string outputBaseDir = "./x4_unpacked";
        bool validateHash = false;

        if (args.Length > 0) inputPath = args[0];
        if (args.Length > 1) outputBaseDir = args[1];
        if (args.Length > 2) bool.TryParse(args[2], out validateHash);

        List<string> catFilesToProcess = [];
        bool isBatchMode = false;

        if (File.Exists(inputPath))
        {
            catFilesToProcess.Add(inputPath);
        }
        else if (Directory.Exists(inputPath))
        {
            isBatchMode = true;
            Console.WriteLine($"Scanning '{Path.GetFullPath(inputPath)}' for .cat files...");

            try
            {
                var files = Directory.GetFiles(inputPath, "*.cat", SearchOption.AllDirectories);

                // Sort Alphabetically to ensure correct load order (01 < 02 < ext...)
                // This sort is CRITICAL for the "Winner-Takes-All" logic to work.
                var sortedFiles = files
                    .Where(f => !f.EndsWith("_sig.cat", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                catFilesToProcess.AddRange(sortedFiles);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error scanning directory: {ex.Message}");
                return;
            }
        }
        else
        {
            Console.WriteLine($"Error: Input path '{inputPath}' not found.");
            return;
        }

        if (catFilesToProcess.Count == 0)
        {
            Console.WriteLine("No .cat files found to unpack.");
            return;
        }

        Console.WriteLine($"Found {catFilesToProcess.Count} catalogs.");
        Console.WriteLine($"Output Directory: {Path.GetFullPath(outputBaseDir)}");
        Console.WriteLine();

        Console.WriteLine("Phase 1: Indexing files to resolve collisions (Winner-Takes-All)...");

        Dictionary<string, (string SourceCat, string Hash, long Size, long Timestamp)> ownershipMap =
            [];

        int indexedFiles = 0;
        foreach (var catPath in catFilesToProcess)
        {

            string datPath = Path.ChangeExtension(catPath, ".dat");
            if (!File.Exists(datPath)) continue;

            string specificOutputDir = outputBaseDir;
            if (isBatchMode)
            {
                string relativeDir = Path.GetRelativePath(inputPath, Path.GetDirectoryName(catPath) ?? inputPath);
                specificOutputDir = Path.Combine(outputBaseDir, relativeDir);
            }

            foreach (string line in File.ReadLines(catPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (TryParseCatLine(line, out CatEntry entry))
                {
                    string fullPath = Path.Combine(specificOutputDir, entry.FilePath);
                    // Last one wins (Load Order). 
                    // If 03.cat has the same file twice, the second one overwrites the entry here.
                    ownershipMap[fullPath] = (catPath, entry.Hash, entry.Size, entry.Timestamp);
                    indexedFiles++;
                }
            }
        }

        Console.WriteLine($"Total Entries: {indexedFiles}. Unique/Final Files: {ownershipMap.Count}");
        Console.WriteLine("Phase 2: Extracting...");
        Console.WriteLine();

        int processedCount = 0;
        int completedCount = 0;
        int totalCatalogs = catFilesToProcess.Count;

        Parallel.ForEach(catFilesToProcess, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (catPath) =>
        {
            int currentIdx = Interlocked.Increment(ref processedCount);
            string displayName = isBatchMode ? Path.GetRelativePath(inputPath, catPath) : Path.GetFileName(catPath);
            string datPath = Path.ChangeExtension(catPath, ".dat");

            string specificOutputDir = outputBaseDir;
            if (isBatchMode)
            {
                string relativeDir = Path.GetRelativePath(inputPath, Path.GetDirectoryName(catPath) ?? inputPath);
                specificOutputDir = Path.Combine(outputBaseDir, relativeDir);
            }

            if (!File.Exists(datPath))
            {
                Interlocked.Increment(ref completedCount);
                lock (_consoleLock) Console.WriteLine($"[Skipping] {displayName} - Missing .dat");
                return;
            }

            lock (_consoleLock) Console.WriteLine($"[{currentIdx}/{totalCatalogs}] STARTING {displayName}...");

            try
            {
                var (extracted, skipped, masked) = ProcessCatalog(catPath, datPath, specificOutputDir, ownershipMap, validateHash);

                int finishedNow = Interlocked.Increment(ref completedCount);
                int remaining = totalCatalogs - finishedNow;

                lock (_consoleLock) Console.WriteLine($"[{currentIdx}/{totalCatalogs}] FINISHED {displayName} (New: {extracted}, Skipped: {skipped}, Masked: {masked}) - {remaining} left");
            }
            catch (Exception ex)
            {
                lock (_consoleLock) Console.Error.WriteLine($"[ERROR] Failed {displayName}: {ex.Message}");
                Interlocked.Increment(ref completedCount);
            }
        });

        Console.WriteLine("\nBatch operation complete.");
    }

    static (int extracted, int skipped, int masked) ProcessCatalog(string catPath, string datPath, string outputDir, Dictionary<string, (string SourceCat, string Hash, long Size, long Timestamp)> ownershipMap, bool validateHash)
    {
        int extracted = 0;
        int skipped = 0;
        int masked = 0;
        long currentOffset = 0;

        string[] catLines = File.ReadAllLines(catPath);

        using (FileStream datStream = new(datPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            foreach (var line in catLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!TryParseCatLine(line, out CatEntry entry)) continue;

                string fullOutputPath = Path.Combine(outputDir, entry.FilePath);

                bool isOwner = false;
                if (ownershipMap.TryGetValue(fullOutputPath, out var winner))
                {

                    if (winner.SourceCat == catPath &&
                        winner.Hash == entry.Hash &&
                        winner.Size == entry.Size &&
                        winner.Timestamp == entry.Timestamp)
                    {
                        isOwner = true;
                    }
                }

                if (!isOwner)
                {
                    // "Masked": Hidden by a newer version (in a later catalog OR later in this same catalog).
                    // We skip IO but MUST increment offset to keep alignment.
                    masked++;
                    currentOffset += entry.Size;
                    continue;
                }

                bool isUpToDate = false;
                if (File.Exists(fullOutputPath))
                {
                    FileInfo fi = new(fullOutputPath);
                    if (fi.Length == entry.Size)
                    {
                        if (entry.Timestamp > 0)
                        {
                            DateTime expectedUtc = DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).UtcDateTime;
                            if (Math.Abs((fi.LastWriteTimeUtc - expectedUtc).TotalSeconds) <= 2)
                            {
                                isUpToDate = true;
                            }
                        }
                        else
                        {
                            // If timestamp is 0 or missing, but size matches, consider it up-to-date.
                            isUpToDate = true;
                        }
                    }
                }

                if (isUpToDate)
                {
                    skipped++;
                    currentOffset += entry.Size;
                    continue;
                }

                string? dir = Path.GetDirectoryName(fullOutputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                ExtractFileChunk(datStream, currentOffset, entry, fullOutputPath, validateHash);
                extracted++;
                currentOffset += entry.Size;
            }
        }
        return (extracted, skipped, masked);
    }

    static void ExtractFileChunk(FileStream source, long offset, CatEntry entry, string destPath, bool validateHash)
    {
        source.Seek(offset, SeekOrigin.Begin);

        byte[] buffer = new byte[Math.Min(BufferSize, entry.Size)];
        long bytesRemaining = entry.Size;

        using (FileStream fsDest = new(destPath, FileMode.Create, FileAccess.Write))
        {
            if (validateHash)
            {
                using MD5 md5 = MD5.Create();
                using CryptoStream cs = new(fsDest, md5, CryptoStreamMode.Write);
                CopyStream(source, cs, bytesRemaining, buffer);
                cs.FlushFinalBlock();

                string actualHash = BitConverter.ToString(md5.Hash!).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actualHash, entry.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    lock (_consoleLock) Console.WriteLine($"\n   [HASH ERROR] {entry.FilePath} (Expected: {entry.Hash}, Got: {actualHash})");
                }
            }
            else
            {
                CopyStream(source, fsDest, bytesRemaining, buffer);
            }
        }

        if (entry.Timestamp > 0)
        {
            try
            {
                // Use UTC to set time
                DateTime dt = DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).UtcDateTime;
                File.SetLastWriteTimeUtc(destPath, dt);
            }
            catch
            {
                // If setting time fails (permissions/FS issues), we just ignore it.
                // The file will just have the current time.
            }
        }
    }

    static void CopyStream(Stream input, Stream output, long bytesToCopy, byte[] buffer)
    {
        while (bytesToCopy > 0)
        {
            int bytesToRead = (int)Math.Min(buffer.Length, bytesToCopy);
            int bytesRead = input.Read(buffer, 0, bytesToRead);
            if (bytesRead == 0) break;
            output.Write(buffer, 0, bytesRead);
            bytesToCopy -= bytesRead;
        }
    }

    static bool TryParseCatLine(string line, out CatEntry entry)
    {
        entry = new CatEntry();
        string[] tokens = line.Split(' ');
        if (tokens.Length < 4) return false;

        entry.Hash = tokens[^1];
        if (!long.TryParse(tokens[^2], out long timestamp)) return false;
        entry.Timestamp = timestamp;
        if (!long.TryParse(tokens[^3], out long size)) return false;
        entry.Size = size;
        entry.FilePath = string.Join(" ", tokens.Take(tokens.Length - 3));
        return true;
    }
}

struct CatEntry
{
    public string FilePath;
    public long Size;
    public long Timestamp;
    public string Hash;
}
