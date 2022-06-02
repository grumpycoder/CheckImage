using System.CommandLine;
using System.CommandLine.Parsing;

var fileOption = new Option<FileInfo>(
    name: "--file",
    description: "Check image input file",
    parseArgument: result =>
    {
        var filePath = result.Tokens.Single().Value;
        if (!File.Exists(filePath))
        {
            result.ErrorMessage = "File does not exist";
            return null;
        }
        return new FileInfo(filePath);
    });
fileOption.AddAlias("-f");
fileOption.IsRequired = true; 

var outputOption = new Option<DirectoryInfo>(
    name: "--output", 
    description: "Directory to extract"    );
outputOption.AddAlias("-o");
outputOption.IsRequired = true; 

var root = new RootCommand("Command Line Utility for X9 formatted Check Image File processing");
root.AddGlobalOption(fileOption);

var validateCommand = new Command("validate", "Validate Check Image x9File");
root.AddCommand(validateCommand);
validateCommand.SetHandler(
    (FileInfo file) => { FileService.Validate(file); }, fileOption);

var mailCommand = new Command("mail", "Email customer confirmation");
root.AddCommand(mailCommand);
mailCommand.SetHandler(
    (FileInfo file) => { MailService.Mail(file); }, fileOption);

var extractCommand = new Command("extract", "Extract images from Check Image x9File");
extractCommand.AddOption(outputOption);
root.AddCommand(extractCommand);

extractCommand.SetHandler(
    (FileInfo file, DirectoryInfo output) => { FileService.Extract(file, output); }, fileOption, outputOption);

return await root.InvokeAsync(args);