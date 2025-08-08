namespace Solution;

public class Hub
{
    private static string path = @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories";

    public static void PrintDirectories()
    {
        var directories = Directory.GetDirectories(path);
        foreach (var dir in directories)
        {
            Console.WriteLine(dir);
        }
    }
}