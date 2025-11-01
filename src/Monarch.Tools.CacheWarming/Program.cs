// C# Tool to warm-up the filesystem cache for .NET 9
// Author: Gemini
// Date: 2025-08-17

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ShellProgressBar;

// --- Configuration ---
const int BufferSize = 81920; // 80 KB buffer, a common size that works well with L2/L3 caches.

// --- Main Execution ---
string startPath = Directory.GetCurrentDirectory();
Console.WriteLine($"  Starting filesystem cache warm-up...");
Console.WriteLine($"   Root Directory: {startPath}");
Console.WriteLine($"   Ignoring symbolic links and junctions.");
Console.WriteLine("-------------------------------------------------");

long totalBytesRead = 0;
long totalBytes = 0;
long filesProcessed = 0;
long directoriesScanned = 0;
long accessErrors = 0;

var stopwatch = Stopwatch.StartNew();

// --- File Discovery Phase ---
var allFiles = new List<FileInfo>();
Console.WriteLine("  Discovering files...");
DiscoverFiles(new DirectoryInfo(startPath), allFiles);
Console.WriteLine($"  Discovery complete. Found {allFiles.Count:N0} files.");

// --- Processing Phase with Progress Bar ---
var progressBarOptions = new ProgressBarOptions
{
    ForegroundColor = ConsoleColor.Yellow,
    BackgroundColor = ConsoleColor.DarkGray,
};

using (var pbar = new ProgressBar((int)((long)totalBytes/1024/1024), "Warming up files...", progressBarOptions))
{
    foreach (var fileInfo in allFiles)
    {
        ProcessFile(fileInfo, pbar);
    }
}


stopwatch.Stop();

// --- Final Report ---
Console.WriteLine("-------------------------------------------------");
Console.WriteLine("  Warm-up complete.");
Console.WriteLine($"   Directories Scanned: {directoriesScanned:N0}");
Console.WriteLine($"   Files Processed:     {filesProcessed:N0}");
Console.WriteLine($"   Total Bytes Read:    {totalBytesRead / 1024.0 / 1024.0:N2} MB");
Console.WriteLine($"   Access Errors:       {accessErrors:N0}");
Console.WriteLine($"   Elapsed Time:        {stopwatch.Elapsed.TotalSeconds:N2} seconds");


// --- Local Functions for Logic ---

/// <summary>
/// Recursively discovers all files in a directory and its subdirectories.
/// </summary>
void DiscoverFiles(DirectoryInfo dirInfo, List<FileInfo> fileList)
{
    Interlocked.Increment(ref directoriesScanned);

    try
    {
        // 1. Add all files in the current directory to the list.
        foreach (var fileInfo in dirInfo.EnumerateFiles())
        {
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                fileList.Add(fileInfo);
                totalBytes += fileInfo.Length;
            }
        }

        // 2. Recursively discover files in subdirectories.
        foreach (var subDirInfo in dirInfo.EnumerateDirectories())
        {
            if ((subDirInfo.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                DiscoverFiles(subDirInfo, fileList);
            }
            else
            {
                // Optional: Log skipped symlinks if needed
                // Console.WriteLine($"   -> Skipping junction/symlink: {subDirInfo.FullName}");
            }
        }
    }
    catch (UnauthorizedAccessException)
    {
        Interlocked.Increment(ref accessErrors);
    }
    catch (Exception ex)
    {
        Interlocked.Increment(ref accessErrors);
        Console.WriteLine($"   [ERROR] Unexpected error during discovery in '{dirInfo.FullName}': {ex.Message}");
    }
}

/// <summary>
/// Opens a file, reads its entire content into a buffer chunk by chunk, and discards it.
/// </summary>
void ProcessFile(FileInfo fileInfo, IProgressBar pbar)
{
    try
    {
        using var stream = fileInfo.OpenRead();
        byte[] buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            Interlocked.Add(ref totalBytesRead, bytesRead);
        }
        totalBytesRead += bytesRead;
        pbar.Tick((int)((long)totalBytesRead/1024/1024), "Continued reading...");
        Interlocked.Increment(ref filesProcessed);
    }
    catch (UnauthorizedAccessException)
    {
        Interlocked.Increment(ref accessErrors);
        pbar.Message = $"   [ACCESS ERROR] Could not read file '{fileInfo.FullName}'";
    }
    catch (IOException ex)
    {
        Interlocked.Increment(ref accessErrors);
        pbar.Message = $"   [IO ERROR] Could not read file '{fileInfo.FullName}': {ex.Message}";
    }
}