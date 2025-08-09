using System.Security.Cryptography;

namespace Solution;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Startup: console test");
        Console.Out.Flush();
        Synchronize(args);
    }

    /* Main sync method; Synchronize the given directories source and replica in args.
 Infinite loop with breaks with given interval.
 Before each loop validating the program did not break and log file still exists.*/
    private static void Synchronize(string[] args)
    {
        try
        {
            ValidatePaths(args);
            var sourceDirectory = args[0];
            var replicaDirectory = args[1];
            var logFile = EnsureLogFile(args.Length == 4 ? args[3] : null);


            while (true)
            {
                ValidatePaths(args);
                logFile = EnsureLogFile(logFile);
                MatchDirectories(sourceDirectory, replicaDirectory, logFile);
                SynchronizeFiles(sourceDirectory, replicaDirectory, logFile);
                Thread.Sleep(int.Parse(args[2]) * 1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void Log(string message, string logFile)
    {
        var timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {message}";
        File.AppendAllText(logFile, timestamped + Environment.NewLine);
    }

    /* Ensuring log file exists. If the given path is empty checking if such file exists from past runtimes, if not
 creating one log file called documentation.log in bin file of the project. If given path is not empty but not valid
then creating the same documentation.log file in bin. Before creation of both actions program checks all the program
files recursively to find any kind of .log file. */
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

            using (File.Create(logFilePath))
            {
            }

            Console.WriteLine($"Log file created in path {logFilePath}");
            Log("Log file created in path " + logFilePath, logFilePath);

            return logFilePath;
        }

        if (!File.Exists(providedPath))
        {
            string? foundPath = Directory
                .EnumerateFiles(basePath, logFileName, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (foundPath != null)
            {
                return foundPath;
            }

            using (File.Create(logFilePath))
            {
            }

            Console.WriteLine($"Log file was not present in path {providedPath}, it created in path {logFilePath}");
            Log($"Log file was not present in path {providedPath}, it created in path " + logFilePath, logFilePath);
            return logFilePath;
        }

        return providedPath;
    }


    /* Deleting extra files or directories in given paths source and replica.
 First deletes the extra files located in replica but not in source,
 then deletes the directories existing in replica but not in source. */
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
            var sourceMatchingDirectory = Path.Combine(sourceDirectory, replicaSubDirectoryName);

            if (!Directory.Exists(sourceMatchingDirectory))
            {
                DeleteDirectory(replicaSubDirectory, logFile);
            }
        }
    }

    /* Matching directories with given path source and replica before starting the sync operation.
 Missing directory makes impossible to copy, update, delete files between directories.*/
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

    /* Synchronize files with given two directories; source and replica.
 Using MD5 hashing to determine if they are different and logging any action taken.*/
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

    /* Copying file from source to replica. If source missing throwing ArgumentException error.*/
    private static void CopyFile(string source, string replica, string logFile)
    {
        if (!File.Exists(source))
            throw new ArgumentException("Source file can not be null. Check the given path.\n" +
                                        "Source path: " + source);
        File.Copy(source, replica);
        Console.WriteLine($"Copied: {source} -> {replica}");
        Log($"Copied: {source} -> {replica}", logFile);
    }

    /* Deleting file if given path contains a file, otherwise throwing ArgumentException error. */
    private static void DeleteFile(string file, string logFile)
    {
        if (!File.Exists(file))
            throw new ArgumentException("Given file to delete does not exists. Check the given path.\n" +
                                        "Path: " + file);
        File.Delete(file);
        Console.WriteLine($"File in path {file} is deleted successfully.");
        Log($"File in path {file} is deleted successfully.", logFile);
    }

    /* Recursive:true ensures that it also deletes the directory even though it is populated with files.
    Deleting directory with given path (directory) if such directory exists. */
    private static void DeleteDirectory(string directory, string logFile)
    {
        if (!Directory.Exists(directory))
            throw new ArgumentException("Given directory to delete does not exists. Check the given path.\n" +
                                        "Path: " + directory);
        Directory.Delete(directory, recursive: true);
        Console.WriteLine($"Directory in path {directory} is deleted successfully.");
        Log($"Directory in path {directory} is deleted successfully.", logFile);
    }

    /* Updating file content: from source file to replica file provided with path. */
    private static void UpdateFile(string source, string replica, string logFile)
    {
        if (File.Exists(source) && File.Exists(replica))
        {
            File.Copy(source, replica, true);

            Console.WriteLine($"Updated: {source} -> {replica}");
            Log($"Updated: {source} -> {replica}", logFile);
        }
        else if (File.Exists(source) && !File.Exists(replica))
        {
            CopyFile(source, replica, logFile);
        }
        else
            throw new ArgumentException(
                "Source directory or replica directory does not exist. Please check provided paths \n"
                + "Replica: " + replica + "\n" + "Source: " + source);
    }

    /* MD5 Hashing used to determine if the files are different. Different size, content would change the hash of files.*/
    private static byte[] MD5Hash(string file)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(file))
        {
            return md5.ComputeHash(stream);
        }
    }

    /* Validation of the given paths, if they are null or empty. If given paths actually lead to existing files and given time interval is actually positive integer */
    private static void ValidatePaths(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length is < 3 or > 4)
            throw new ArgumentException("Usage: [sourcePath] [replicaPath] [intervalInSeconds] [logFilePath?]");

        var sourcePath = args[0];
        var replicaPath = args[1];
        var interval = args[2];

        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source directory path cannot be empty.");
        if (string.IsNullOrWhiteSpace(replicaPath))
            throw new ArgumentException("Replica directory path cannot be empty.");
        if (string.IsNullOrWhiteSpace(interval))
            throw new ArgumentException("Interval cannot be empty.");

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException("Source directory does not exist: " + sourcePath);
        if (!Directory.Exists(replicaPath))
            throw new DirectoryNotFoundException("Replica directory does not exist: " + replicaPath);

        if (String.Equals(sourcePath, replicaPath))
            throw new ArgumentException("Source and Replica directories must be different than each other.");

        bool isNumber = int.TryParse(interval, out var intervalInSeconds);
        if (!isNumber || intervalInSeconds <= 0)
        {
            throw new ArgumentException("Interval must be a positive integer.");
        }
    }
}