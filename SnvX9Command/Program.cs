using System.CommandLine;
using System.Text;
using CompAnalytics.X9;
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

var outputOption = new Option<DirectoryInfo?>(
    name: "--output",
    description: "Path to extract images");
outputOption.AddAlias("-o");

    
var rootCommand = new RootCommand("Command Line Utility for X9 formatted Check Image File processing");
rootCommand.AddGlobalOption(fileOption);

var validateCommand = new Command("validate", "Validate Check Image file");
rootCommand.AddCommand(validateCommand);

validateCommand.SetHandler(
    (FileInfo file) => { ValidateFile(file); }, fileOption);

var extractCommand = new Command("extract", "Extract images from Check Image file"); 
extractCommand.AddOption(outputOption);
rootCommand.AddCommand(extractCommand);

extractCommand.SetHandler((FileInfo input, DirectoryInfo output) => { ExtractImages(input, output); }, fileOption, outputOption);

var emailConfirmationCommand = new Command("email-confirm", "Email customer confirmation");
rootCommand.AddCommand(emailConfirmationCommand);

emailConfirmationCommand.SetHandler((FileInfo input) => { EmailConfirmation(input); }, fileOption);

return await rootCommand.InvokeAsync(args);

static void ValidateFile(FileSystemInfo file)
{
    Console.WriteLine($"Starting: Validating file {file.FullName} ...");
    Console.WriteLine(!ValidateCidFile(file)
        ? $"Error: {file.Name} validation failed"
        : $"Completed Validation succeeded {file.FullName}");
}

static void ExtractImages(FileSystemInfo file, DirectoryInfo? output)
{
    try
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        output ??= new DirectoryInfo(file.FullName.Replace(file.Extension, ""));
        Console.WriteLine($"Starting: Extracting images {file.Name} to {output} ...");

        using Stream x9File = File.OpenRead(file.FullName);
        using X9Reader reader = new X9Reader(x9File);
        var doc = reader.ReadX9Document();
        reader.WriteImagesToDisk(output);
    
        Console.WriteLine($"Completed: Extracting images {file.Name} to {output} ...");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error: Extracting images failed {file.Name} {e.Message} ");
    }

}

static void EmailConfirmation(FileSystemInfo file)
{
    Console.WriteLine($"Starting: Confirmation email {file.FullName} ...");
    if (!ValidateCidFile(file))
    {
        Console.WriteLine($"Error: {file.Name} failed validation. Email NOT sent");
        return;
        
    }
    Console.WriteLine($"Completed: Confirmation email {file.FullName} ...");
}

static bool ValidateCidFile(FileSystemInfo file)
{
    try
    {
        var processor = new Processor();
        processor.Execute(new FileStream(file.FullName, FileMode.Open));
        return true; 
    }
    catch (Exception e)
    {
        return false; 
    }
}
