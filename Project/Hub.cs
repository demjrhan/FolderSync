using System.Security.Cryptography;
using Microsoft.VisualBasic.CompilerServices;

namespace Solution;

public class Hub
{
    private static string log;

    public static void Log(string message)
    {
        string timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {message}";
        File.AppendAllText(log, timestamped + Environment.NewLine);
    }

    public static void Synchronize(string[] args)
    {
        ValidatePaths(args);

        var sourceDirectory = args[0];
        var replicaDirectory = args[1];
        log = args[3];

        MatchDirectories(sourceDirectory, replicaDirectory);
        while (true)
        {
            SynchronizeFiles(sourceDirectory,replicaDirectory);
            Thread.Sleep(int.Parse(args[2]) * 1000);
        }
    }

    private static void DeleteExtras(string sourceDirectoryPath, string replicaDirectoryPath)
    {
        var replicaFiles = Directory.GetFiles(replicaDirectoryPath);
        foreach (var replicaFilePath in replicaFiles)
        {
            var replicaFileName = Path.GetFileName(replicaFilePath);
            var sourceFilePath = Path.Combine(sourceDirectoryPath, replicaFileName);

            if (!File.Exists(sourceFilePath))
            {
                DeleteFile(replicaFilePath);
            }
        }
        var replicaDirectories = Directory.GetDirectories(replicaDirectoryPath);
        foreach (var replicaDirectory in replicaDirectories)
        {
            var replicaDirectoryName = Path.GetFileName(replicaDirectory);
            var toDeleteDirectory = Path.Combine(sourceDirectoryPath, replicaDirectoryName);

            if (!Directory.Exists(toDeleteDirectory))
            {
                DeleteDirectory(replicaDirectory);
            }
        }
    }

    private static void MatchDirectories(string sourceDirectoryPath, string replicaDirectoryPath)
    {
        var sourceDirectories = Directory.GetDirectories(sourceDirectoryPath);
        foreach (var directory in sourceDirectories)
        {
            var sourceDirectoryDirectoryName = Path.GetFileName(directory);
            var replicaDirectoryDirectoryName = Path.Combine(replicaDirectoryPath, sourceDirectoryDirectoryName);
            if (!File.Exists(replicaDirectoryDirectoryName))
                Directory.CreateDirectory(replicaDirectoryDirectoryName);
            MatchDirectories(directory,replicaDirectoryDirectoryName);
        }
    }

    private static void SynchronizeFiles(string sourceDirectoryPath, string replicaDirectoryPath)
    {
        DeleteExtras(sourceDirectoryPath, replicaDirectoryPath);
        var sourceFiles = Directory.GetFiles(sourceDirectoryPath);

        foreach (var sourceFilePath in sourceFiles)
        {
            var sourceFileName = Path.GetFileName(sourceFilePath);
            var replicaFilePath = Path.Combine(replicaDirectoryPath, sourceFileName);

            if (!File.Exists(replicaFilePath))
            {
                CopyFile(sourceFilePath, replicaFilePath);
            }
            else
            {
                var sourceHash = MD5Hash(sourceFilePath);
                var replicaHash = MD5Hash(replicaFilePath);

                var sourceLastWrite = File.GetLastWriteTimeUtc(sourceFilePath);
                var replicaLastWrite = File.GetLastWriteTimeUtc(replicaFilePath);
                
                
                if (!sourceHash.SequenceEqual(replicaHash) || sourceLastWrite > replicaLastWrite)
                {
                    UpdateFile(sourceFilePath, replicaFilePath);
                }
            }
        }

        var sourceDirectories = Directory.GetDirectories(sourceDirectoryPath);
        foreach (var directory in sourceDirectories)
        {
            var sourceDirectoryName = Path.GetFileName(directory);
            var replicaDirectoryName = Path.Combine(replicaDirectoryPath, sourceDirectoryName);
            SynchronizeFiles(directory,replicaDirectoryName);
        }
    }


    private static void CopyFile(string source, string replica)
    {
        File.Copy(source, replica);
        Log($"Copied: {source} -> {replica}");
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Log($"File in path {path} is deleted successfully.");
        }
    }
    
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path);
            Log($"Directory in path {path} is deleted successfully.");
        }
    }

    private static void UpdateFile(string source, string replica)
    {
        File.Copy(source, replica, true);
        Log($"Updated: {source} -> {replica}");
    }

    private static byte[] MD5Hash(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            return md5.ComputeHash(stream);
        }
    }


    private static void ValidatePaths(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: [sourcePath] [replicaPath] [intervalInSeconds] [logFilePath]");
            Environment.Exit(1);
        }

        if (!Directory.Exists(args[0]))
        {
            Console.WriteLine("Source directory does not exist: " + args[0]);
            Environment.Exit(1);
        }

        if (!Directory.Exists(args[1]))
        {
            Console.WriteLine("Replica directory does not exist: " + args[1]);
            Environment.Exit(1);
        }

        if (!File.Exists(args[3]))
        {
            Console.WriteLine("Replica directory does not exist: " + args[3]);
            Environment.Exit(1);
        }

        bool isNumber = int.TryParse(args[2], out var intervalInSeconds);

        if (!isNumber || intervalInSeconds <= 0)
        {
            Console.WriteLine("Interval must be a positive integer.");
            Environment.Exit(1);
        }
    }
}