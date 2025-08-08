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
                
                if (!sourceHash.SequenceEqual(replicaHash))
                {
                    UpdateFile(sourceFilePath, replicaFilePath);
                }                
            }
            
        }
    }
    
    
    public static void CopyFile(string source, string replica)
    {
        File.Copy(source, replica);
        Log($"Copied: {source} -> {replica}");
    }

    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Log($"File in path {path} is deleted successfully.");
        } 
    }

    public static void UpdateFile(string source, string replica)
    {
        File.Copy(source, replica, true);
        Log($"Updated: {source} -> {replica}");

    }

    private static byte[] MD5Hash(string filePath)
    {
        using(var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            return md5.ComputeHash(stream);
        }
    }

    private static bool ValidateIfFileExists(string sourceFilePath, string replicaDirectoryPath)
    {
        var files = Directory.GetFiles(replicaDirectoryPath);
        string sourceFileName = Path.GetFileName(sourceFilePath);

        foreach (var file in files)
        {
            if (Path.GetFileName(file).Equals(sourceFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ValidateIfDirectoryExists(string sourceDirectoryPath, string replicaDirectoryPath)
    {
        string sourceDirName = Path.GetFileName(sourceDirectoryPath);
        var directories = Directory.GetDirectories(replicaDirectoryPath);

        foreach (var dir in directories)
        {
            if (Path.GetFileName(dir).Equals(sourceDirName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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