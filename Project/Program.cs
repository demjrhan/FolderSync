using Solution;

class Program
{
    static void Main(string[] args)
    {
        string[] input = new[]
        {
            @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\Source",
            @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\Replica",
            "1",
            @"C:\Users\demir\Documents\personal\FolderSync\Project\Directories\log.txt"
        };
        Hub.Synchronize(input);
    }
}