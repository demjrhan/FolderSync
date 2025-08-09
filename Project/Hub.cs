using System.Security.Cryptography;

namespace Solution;

public class Hub
{
    public static void Synchronize(string[] args)
    {
        ValidatePaths(args);

        var sourceDirectory = args[0];
        var replicaDirectory = args[1];
        var logFile = EnsureLogFile(args.Length == 4 ? args[3] : null);
        
        
        while (true)
        {
            EnsureLogFile(null);
            MatchDirectories(sourceDirectory, replicaDirectory, logFile);
            SynchronizeFiles(sourceDirectory, replicaDirectory, logFile);
            Thread.Sleep(int.Parse(args[2]) * 1000);
        }
    }

    private static void Log(string message, string logFile)
    {
        var timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {message}";
        File.AppendAllText(logFile, timestamped + Environment.NewLine);
    }

    private static string EnsureLogFile(string? providedPath)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string logFileName = "documentation.log";
        string logFilePath = Path.Combine(basePath, logFileName);

        if (string.IsNullOrWhiteSpace(providedPath))
        {
            if (File.Exists(logFilePath))
            {
                return logFilePath;
            }

            string? foundPath = Directory
                .EnumerateFiles(basePath, logFileName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (foundPath != null)
            {
                return foundPath;
            }

            Console.WriteLine("Log file created in path " + logFilePath);
            using (File.Create(logFilePath)) { }
            return logFilePath;
        }

        if (!File.Exists(providedPath))
        {
            using (File.Create(logFilePath)) { }
            Console.WriteLine($"Log file was not present in path {providedPath}, it created in path " + logFilePath);
            return logFilePath;
        }

        return providedPath;
    }

    private static void DeleteExtras(string sourceDirectory, string replicaDirectory, string logFile)
    {
        var replicaFiles = Directory.GetFiles(replicaDirectory);
        foreach (var replicaFile in replicaFiles)
        {
            var replicaFileName = Path.GetFileName(replicaFile);
            var sourceFilePath = Path.Combine(sourceDirectory, replicaFileName);

            if (!File.Exists(sourceFilePath))
            {
                DeleteFile(replicaFile, logFile);
            }
        }

        var replicaSubDirectories = Directory.GetDirectories(replicaDirectory);
        foreach (var replicaSubDirectory in replicaSubDirectories)
        {
            var replicaSubDirectoryName = Path.GetFileName(replicaSubDirectory);
            var toDeleteDirectory = Path.Combine(sourceDirectory, replicaSubDirectoryName);

            if (!Directory.Exists(toDeleteDirectory))
            {
                DeleteDirectory(replicaSubDirectory, logFile);
            }
        }
    }

    private static void MatchDirectories(string sourceDirectory, string replicaDirectory, string logFile)
    {
        var sourceSubDirectories = Directory.GetDirectories(sourceDirectory);
        foreach (var sourceSubDirectory in sourceSubDirectories)
        {
            var sourceSubDirectoryName = Path.GetFileName(sourceSubDirectory);
            var replicaSubDirectory = Path.Combine(replicaDirectory, sourceSubDirectoryName);
            if (!Directory.Exists(replicaSubDirectory))
            {
                Directory.CreateDirectory(replicaSubDirectory);
                Log($"Directory created: {replicaSubDirectory}", logFile);
            }

            MatchDirectories(sourceSubDirectory, replicaSubDirectory, logFile);
        }
    }

    private static void SynchronizeFiles(string sourceDirectory, string replicaDirectory, string logFile)
    {
        DeleteExtras(sourceDirectory, replicaDirectory, logFile);
        var sourceFiles = Directory.GetFiles(sourceDirectory);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileName = Path.GetFileName(sourceFile);
            var replicaFile = Path.Combine(replicaDirectory, sourceFileName);

            if (!File.Exists(replicaFile))
            {
                CopyFile(sourceFile, replicaFile, logFile);
            }
            else
            {
                var sourceHash = MD5Hash(sourceFile);
                var replicaHash = MD5Hash(replicaFile);

                var sourceLastWrite = File.GetLastWriteTimeUtc(sourceFile);
                var replicaLastWrite = File.GetLastWriteTimeUtc(replicaFile);


                if (!sourceHash.SequenceEqual(replicaHash) || sourceLastWrite > replicaLastWrite)
                {
                    UpdateFile(sourceFile, replicaFile, logFile);
                }
            }
        }

        var sourceSubDirectories = Directory.GetDirectories(sourceDirectory);
        foreach (var sourceSubDirectory in sourceSubDirectories)
        {
            var sourceSubDirectoryName = Path.GetFileName(sourceSubDirectory);
            var replicaSubDirectory = Path.Combine(replicaDirectory, sourceSubDirectoryName);
            SynchronizeFiles(sourceSubDirectory, replicaSubDirectory, logFile);
        }
    }


    private static void CopyFile(string source, string replica, string logFile)
    {
        File.Copy(source, replica);
        Log($"Copied: {source} -> {replica}", logFile);
    }

    private static void DeleteFile(string file, string logFile)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
            Log($"File in path {file} is deleted successfully.", logFile);
        }
    }

    /* Recursive:true ensures that it also deletes the directory even though it is populated with files. */
    private static void DeleteDirectory(string directory, string logFile)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory,recursive: true);
            Log($"Directory in path {directory} is deleted successfully.", logFile);
        }
    }

    private static void UpdateFile(string source, string replica, string logFile)
    {
        File.Copy(source, replica, true);
        Log($"Updated: {source} -> {replica}", logFile);
    }

    private static byte[] MD5Hash(string file)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(file))
        {
            return md5.ComputeHash(stream);
        }
    }


    private static void ValidatePaths(string[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            Console.WriteLine("Usage: [sourcePath] [replicaPath] [intervalInSeconds] [logFilePath] -> Optional");
            Environment.Exit(1);
        }
        
        var sourcePath = args[0];
        var replicaPath = args[1];
        var interval = args[2];
        
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine("Source directory path cannot be empty.");
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(replicaPath))
        {
            Console.WriteLine("Replica directory path cannot be empty.");
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(interval))
        {
            Console.WriteLine("Interval cannot be empty.");
            Environment.Exit(1);
        }
        
        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine("Source directory does not exist: " + sourcePath);
            Environment.Exit(1);
        }

        if (!Directory.Exists(replicaPath))
        {
            Console.WriteLine("Replica directory does not exist: " + replicaPath);
            Environment.Exit(1);
        }

        bool isNumber = int.TryParse(interval, out var intervalInSeconds);

        if (!isNumber || intervalInSeconds <= 0)
        {
            Console.WriteLine("Interval must be a positive integer.");
            Environment.Exit(1);
        }
    }
}