using System.CommandLine;
using System.Net.Mail;
using System.Text;
using CompAnalytics.X9;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using SnvX9Command;
using SnvX9Command.Data;
using SnvX9Command.Entities;
using SnvX9Command.Helpers;
using X9;
using X9.Models.FileStructure;

var fileOption = new Option<FileInfo?>(
    name: "--x9File",
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

var validateCommand = new Command("validate", "Validate Check Image x9File");
rootCommand.AddCommand(validateCommand);

validateCommand.SetHandler(
    (FileInfo file) => { ValidateFile(file); }, fileOption);

var extractCommand = new Command("extract", "Extract images from Check Image x9File");
extractCommand.AddOption(outputOption);
rootCommand.AddCommand(extractCommand);

extractCommand.SetHandler((FileInfo input, DirectoryInfo output) => { ExtractImages(input, output); }, fileOption,
    outputOption);

var emailConfirmationCommand = new Command("email-confirm", "Email customer confirmation");
rootCommand.AddCommand(emailConfirmationCommand);

emailConfirmationCommand.SetHandler((FileInfo input) => { EmailConfirmation(input); }, fileOption);

return await rootCommand.InvokeAsync(args);

static void ValidateFile(FileSystemInfo file)
{
    Console.WriteLine($"Starting: Validating x9File {file.FullName} ...");
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
        reader.ReadX9Document();
        reader.WriteImagesToDisk(output);

        Console.WriteLine($"Completed: Extracting images {file.Name} to {output}.");
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

    //get customer x9File
    var x9File = GetSpecFromFile(file);
    //get email address
    var customer = GetCustomer(x9File.FileHeader.ImmediateOriginName.Trim());
    if (customer is null)
    {
        Console.WriteLine($"Error: Customer record not found in database. Email NOT sent");
        return;
    }
    //send message
    SendMail(x9File, file.FullName);
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

static X9File GetSpecFromFile(FileSystemInfo file)
{
    if (!ValidateCidFile(file))
    {
        Console.WriteLine($"Error: {file.Name} failed validation.");
        throw new ApplicationException("Invalid x9File format");
    }

    var processor = new Processor();
    processor.Execute(new FileStream(file.FullName, FileMode.Open));

    return processor.FileSpec;
}

static Customer? GetCustomer(string customerName)
{
    using var context = new CidContext();
    var customer = context.Customers.FirstOrDefault(x => x.LongName == customerName.Trim());

    return customer;
}

static MailMessage CreateMailBody(X9File file, Customer customer)
{
    var filePath = "demo.txt";
    // var tab = "\t";
    // string line = new('-', 30);
    var tab = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
    string line = "<hr />";

    var sb = new StringBuilder();
    sb.Append("Your deposit was received for processing.  Please review the summary information below.<br />");
    sb.Append($"{customer.LongName}<br />");
    sb.Append($"FILENAME: {Path.GetFileName(filePath)}<br />");
    sb.Append($"FILE HEADER<br />{line}<br />");
    sb.Append($"{tab}DATE: {file.FileHeader.FileCreationDate.ToDate()}<br />");
    sb.Append($"{tab}TIME: {file.FileHeader.FileCreationTime.To24HourTime()}<br />");
    sb.Append($"{tab}CLIENT: {file.FileHeader.ImmediateOriginName}<br />");

    //TODO: Should loop thru each Cash Letter? 
    sb.Append($"{tab}CASH LETTER<br /> {tab}{line}<br />");
    sb.Append($"{tab}{tab}CREDIT ACCOUNT: {file.FileControl.ImmediateOriginContactName}<br />");

    //TODO: What is the Credit Serial reference?
    sb.Append($"{tab}{tab}CREDIT SERIAL: {file.CashLetter.CashLetterControl.CashLetterItemCount}<br />");
    sb.Append($"{tab}{tab}CHECK RECORDS: {file.FileControl.TotalItemCount.ToInt()}<br />");
    sb.Append(
        $"{tab}{tab}CHECK IMAGES: {file.CashLetter.CashLetterControl.CashLetterImageViewCount.ToInt()}<br />");
    sb.Append($"{tab}{tab}CHECK TOTAL: {file.FileControl.FileTotalAmount.ToDecimal().ToMoney()}<br />");

    var mailMessage = new MailMessage();
    mailMessage.From = new MailAddress("marklawrence@synovus.com");
    mailMessage.To.Add(customer.ContactEmail);
    mailMessage.Subject = $"Deposit Received: {filePath}";
    mailMessage.IsBodyHtml = true;
    mailMessage.Body = sb.ToString();

    return mailMessage;
}

static void SendMail(X9File x9File, string fileName)
{
    Email.DefaultRenderer = new RazorRenderer();
    var tempDirectory = Path.Combine(Path.GetTempPath(), "EmailTest");

    var sender = new SmtpSender(() => new SmtpClient("localhost")
    {
        EnableSsl = false,
        DeliveryMethod = SmtpDeliveryMethod.Network,
        PickupDirectoryLocation = tempDirectory
    });
    Email.DefaultSender = sender;
    Directory.CreateDirectory(tempDirectory);

    var model = BuildEmailModel(x9File, fileName);
    var customer = GetCustomer(model.CustomerName); 
    var email = Email
        .From("marklawrence@synovus.com")
        .To(customer?.ContactEmail)
        .Subject($"Deposit Received: {model.FileName}")
        .UsingTemplateFromFile("templates\\confirm-email.cshtml", model: model);

    email.Send();

    Console.WriteLine($"Email Sent: {email.Data.ToAddresses[0]} | {email.Data.Subject} ");
}

static EmailModel BuildEmailModel(X9File file, string filePath)
{
    var model = new EmailModel()
    {
        FileName = Path.GetFileName(filePath), 
        CustomerName = file.FileHeader.ImmediateOriginName,
        FileHeaderDate = file.FileHeader.FileCreationDate.ToDate(),
        FileHeaderTime = file.FileHeader.FileCreationTime.To24HourTime()
    };
    foreach (var item in file.CashLetter.Bundles)
    {
        model.CashLetters.Add(new CashLetterModel()
        {
            CreditAccount = item.BundleHeader.BundleClientInstitutionRoutingNumber,
            Serial = "0",
            BundleSequence = item.BundleHeader.BundleSequenceNumber.ToInt(),
            CheckRecordsCount = item.BundleControl.BundleItemCount.ToInt(),
            CheckImagesCount = item.BundleControl.ImagesWithinBundleCount.ToInt(),
            CheckTotal = item.BundleControl.BundleTotalAmount.ToDecimal()
        });
    }

    return model; 
}