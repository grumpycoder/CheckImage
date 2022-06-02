using System.Net.Mail;
using FluentEmail.Core;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using X9;
using X9.Models.FileStructure;

public static class MailService
{
    public static void Mail(FileInfo file)
    {
        if (FileService.Validate(file))
        {
            Console.WriteLine($"Sent confirmation email {file.Name}");
            return;
        }
        
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

        var processor = new Processor();
        processor.Execute(new FileStream(file.FullName, FileMode.Open));
        
        var model = BuildEmailModel(processor.FileSpec, file.Name);
        //var customer = GetCustomer(model.CustomerName); 
        var contactEmail = "marklawrence@synovus.com"; 
        
        var email = Email
            .From("marklawrence@synovus.com")
            .To(contactEmail)
            .Subject($"Deposit Received: {model.FileName}")
            .UsingTemplateFromFile("templates\\confirm-email.cshtml", model: model);

        email.Send();
        Console.WriteLine($"Sent validation error email {file.Name}");
    }
    
    private static EmailModel BuildEmailModel(X9File file, string filePath)
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
    
}