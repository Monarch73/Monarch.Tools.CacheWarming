# Monarch75.Tools.CacheWarming
 

## Installation


Install the tool globally using the following command:
```
dotnet tool install Monarch75.Tools.CacheWarming -g
```

  

## Usage

  

1. Change to the directory you want to warm the cache for:

```
cd <your-directory>
```

  

2. Run the tool:

```
cachewarmup
```
  

The tool will recursively read all files in the current working directory. The data read will be discarded (not deleted).