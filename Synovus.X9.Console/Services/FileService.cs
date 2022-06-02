using System.Text;
using CompAnalytics.X9;

public static class FileService
{
    public static bool Validate(FileInfo file)
    {
        Console.WriteLine($"Validate {file.Name}");
        return false; 
    }

    public static void Extract(FileInfo file, DirectoryInfo output)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.WriteLine($"Starting: Extracting images {file.Name} to {output} ...");

        using Stream x9File = File.OpenRead(file.FullName);
        using var reader = new X9Reader(x9File);
        reader.ReadX9Document();
        reader.WriteImagesToDisk(output);

        Console.WriteLine($"Completed: Extracting images {file.Name} to {output}.");
        
    }
}