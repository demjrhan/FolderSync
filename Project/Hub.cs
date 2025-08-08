namespace Solution;

public class Hub
{
    private static string root = @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories";
    private static string replica = @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\Replica";
    private static string source = @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\Source";
    private static string log = @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\log.txt";
    
    public static void PrintDirectories()
    {
        var directories = Directory.GetDirectories(root);
        foreach (var dir in directories)
        {
            Console.WriteLine(dir);
        }
    }

    public static void Synchronize(string[] args)
    {
        ValidatePaths(args);
        Console.WriteLine("All inputs are valid. Starting to sync...");
        /* Continue here for compare, if missing add, if exists update, if extra delete operations with MD5. */
        
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
        
        if (!int.TryParse(args[2], out int intervalInSeconds) || intervalInSeconds <= 0)
        {
            Console.WriteLine("Interval must be a positive integer.");
            Environment.Exit(1);
        }
        
    }
}