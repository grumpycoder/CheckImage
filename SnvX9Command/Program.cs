using System.CommandLine;
using X9;

var fileOption = new Option<FileInfo?>(
    name: "--file",
    description: "Path to Check Image File",
    isDefault: true,
    parseArgument: result =>
    {
        if (result.Tokens.Count == 0)
        {
            return new FileInfo("sampleQuotes.txt");
        }

        var filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "File does not exist";
            return null;
        }
        else
        {
            return new FileInfo(filePath);
        }
    });
fileOption.AddAlias("-f");

var rootCommand = new RootCommand("Command Line Utility for X9 formatted Check Image File processing");
rootCommand.AddGlobalOption(fileOption);

var validateCommand = new Command("validate", "Validate Check Image file");
rootCommand.AddCommand(validateCommand);

validateCommand.SetHandler(
    (FileInfo file) => { ValidateFile(file); }, fileOption);

return await rootCommand.InvokeAsync(args);

static void ValidateFile(FileSystemInfo file)
{
    Console.WriteLine($"Validating file {file.FullName} ...");
    try
    {
        var processor = new Processor();
        processor.Execute(new FileStream(file.FullName, FileMode.Open));
        Console.WriteLine($"{file.Name} validated succeeded");
    }
    catch (Exception e)
    {
        Console.WriteLine($"{file.Name} validation failed");
    }
}