// C# Tool to warm-up the filesystem cache for .NET 9
// Author: Gemini
// Date: 2025-08-17

using System.Diagnostics;
using System.IO;

// --- Configuration ---
const int BufferSize = 81920; // 80 KB buffer, a common size that works well with L2/L3 caches.

// --- Main Execution ---
string startPath = Directory.GetCurrentDirectory();
Console.WriteLine($"🔥 Starting filesystem cache warm-up...");
Console.WriteLine($"   Root Directory: {startPath}");
Console.WriteLine($"   Ignoring symbolic links and junctions.");
Console.WriteLine("-------------------------------------------------");

long totalBytesRead = 0;
long filesProcessed = 0;
long directoriesScanned = 0;
long accessErrors = 0;

var stopwatch = Stopwatch.StartNew();

// Start the recursive processing from the current directory.
var startDirectoryInfo = new DirectoryInfo(startPath);
ProcessDirectory(startDirectoryInfo);

stopwatch.Stop();

// --- Final Report ---
Console.WriteLine("-------------------------------------------------");
Console.WriteLine("✅ Warm-up complete.");
Console.WriteLine($"   Directories Scanned: {directoriesScanned:N0}");
Console.WriteLine($"   Files Processed:     {filesProcessed:N0}");
Console.WriteLine($"   Total Bytes Read:    {totalBytesRead / 1024.0 / 1024.0:N2} MB");
Console.WriteLine($"   Access Errors:       {accessErrors:N0}");
Console.WriteLine($"   Elapsed Time:        {stopwatch.Elapsed.TotalSeconds:N2} seconds");


// --- Local Functions for Logic ---

/// <summary>
/// Recursively processes a directory, its files, and its subdirectories.
/// </summary>
void ProcessDirectory(DirectoryInfo dirInfo)
{
    // Check if the directory is a symbolic link or junction point. If so, skip it.
    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
    {
        Console.WriteLine($"   -> Skipping junction/symlink: {dirInfo.FullName}");
        return;
    }

    Interlocked.Increment(ref directoriesScanned);

    try
    {
        // 1. Process all files in the current directory.
        foreach (var fileInfo in dirInfo.EnumerateFiles())
        {
            // Also check files for the ReparsePoint attribute.
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                ProcessFile(fileInfo);
            }
        }

        // 2. Recursively process all subdirectories.
        foreach (var subDirInfo in dirInfo.EnumerateDirectories())
        {
            ProcessDirectory(subDirInfo);
        }
    }
    catch (UnauthorizedAccessException)
    {
        Interlocked.Increment(ref accessErrors);
        // Silently ignore directories we cannot access. You could add logging here if needed.
    }
    catch (Exception ex)
    {
        Interlocked.Increment(ref accessErrors);
        Console.WriteLine($"   [ERROR] Unexpected error in '{dirInfo.FullName}': {ex.Message}");
    }
}

/// <summary>
/// Opens a file, reads its entire content into a buffer chunk by chunk, and discards it.
/// </summary>
void ProcessFile(FileInfo fileInfo)
{
    try
    {
        // Use a FileStream for efficient reading without loading the whole file into memory.
        using var stream = fileInfo.OpenRead();

        byte[] buffer = new byte[BufferSize];
        int bytesRead;

        // The core read loop.
        // The result is added to 'totalBytesRead' to prevent the JIT compiler
        // from optimizing away this loop because its result is unused.
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            Interlocked.Add(ref totalBytesRead, bytesRead);
        }

        Interlocked.Increment(ref filesProcessed);
    }
    catch (UnauthorizedAccessException)
    {
        Interlocked.Increment(ref accessErrors);
        // Silently ignore files we cannot access.
    }
    catch (IOException ex)
    {
        Interlocked.Increment(ref accessErrors);
        // File might be locked by another process.
        Console.WriteLine($"   [IO ERROR] Could not read file '{fileInfo.FullName}': {ex.Message}");
    }
}