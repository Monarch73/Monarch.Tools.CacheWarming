# Monarch.Tools.CacheWarming

## Project Overview

This project is a .NET tool designed to warm up the file system cache. It recursively reads all files in the current directory to load them into the cache. This is particularly useful for scenarios involving virtual file systems (like rclone) where initial file access can be slow. The tool is written in C# and targets .NET 9.

The main logic is contained within `Program.cs`. It iterates through directories and files, reading file contents into a buffer to force them into the system's file cache. It avoids reading the content of symbolic links and junctions.

The project is configured as a .NET tool, and the tool command name is `cachewarmup`.

## Building and Running

### Build

To build the project, you can use the standard `dotnet build` command:

```bash
dotnet build --configuration Release
```

### Run

The tool is intended to be installed as a global .NET tool.

1.  **Install the tool:**

    ```bash
    dotnet tool install --global --add-source ./nupkg Monarch.Tools.CacheWarming
    ```

2.  **Run the tool:**

    Navigate to the directory you want to warm up and run the tool:

    ```bash
    cachewarmup
    ```

## Development Conventions

*   The project uses the standard C# coding conventions.
*   The project uses file-scoped namespaces.
*   The project uses implicit usings.
*   The project is configured to be a .NET tool.
