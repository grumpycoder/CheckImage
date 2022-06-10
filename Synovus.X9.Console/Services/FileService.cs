using System.Text;
using CompAnalytics.X9;
using X9;
using X9.Common.Extensions;
using X9.Models.FileStructure;

public static class FileService
{
    public static ValidationResult Validate(FileInfo file)
    {
        var validationResult = new ValidationResult(true);
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine($"Validating {file.Name}...");

            var processor = new Processor();
            processor.Execute(new FileStream(file.FullName, FileMode.Open));

            var fileSpec = processor.FileSpec;

            ValidateCashLetterControl(fileSpec, ref validationResult);
            ValidateFileControl(fileSpec, ref validationResult);
            ValidateBundles(fileSpec, ref validationResult);

            validationResult.Status = validationResult.ErrorMessages.Count == 0;
        }
        catch (IOException e)
        {
            validationResult.Status = false;
            validationResult.ErrorMessages.Add($"Unable to access file {file.Name}");
            return validationResult;
        }
        finally
        {
            if (!validationResult.Status)
            {
                Console.WriteLine("Validation Error Results: ");
                foreach (var message in validationResult.ErrorMessages)
                {
                    Console.WriteLine(message);
                }
            }

            if (validationResult.Status)
            {
                Console.WriteLine($"Validation completed...");
            }
            else
            {
                Console.WriteLine($"Validation completed with errors...");
            }
        }

        return validationResult;
    }

    private static void ValidateBundles(X9File fileSpec, ref ValidationResult validationResult)
    {
        var creditRecordsCount = fileSpec.CashLetter.Bundles.Count(x => x.CreditRecord.ItemAmount != null);
        var hasCreditRecords = creditRecordsCount == fileSpec.CashLetter.Bundles.Count;

        if (!hasCreditRecords)
        {
            var errorMessage =
                $"Bundle control records are possibly incorrect. All Bundles do not have a Credit Record.";
            validationResult.ErrorMessages.Add(errorMessage);
        }

        foreach (var bundle in fileSpec.CashLetter.Bundles)
        {
            var bundleId = bundle.BundleHeader.BundleId.Trim();

            if (bundle.CreditRecord.ItemAmount == null)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is possibly incorrect due to missing Credit Record. The bundle does not contain a Credit Record.";
                validationResult.ErrorMessages.Add(errorMessage);
            }

            var bundleControlItemCount = bundle.BundleControl.BundleItemCount.ToInt();
            var bundleCheckItemsCount = bundle.CheckItems.Count;
            if (bundleControlItemCount != bundleCheckItemsCount)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is incorrect. The bundle contains {bundleCheckItemsCount} check items, but record says there should be {bundleControlItemCount} items.";
                validationResult.ErrorMessages.Add(errorMessage);
            }

            var bundleControlTotalAmount = bundle.BundleControl.BundleTotalAmount.ToDecimal();
            var bundleCheckItemsTotalAmount =
                bundle.CheckItems.Sum(checkItem => checkItem.CheckDetail.ItemAmount.ToDecimal());
            if (bundleControlTotalAmount != bundleCheckItemsTotalAmount)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is incorrect. The bundle contains items with a total value of ${bundleCheckItemsTotalAmount}, but record says the total amount should be ${bundleControlTotalAmount}.";
                validationResult.ErrorMessages.Add(errorMessage);
            }
        }
    }

    private static void ValidateCashLetterControl(X9File fileSpec, ref ValidationResult validationResult)
    {
        var creditRecordsCount = fileSpec.CashLetter.Bundles.Count(x => x.CreditRecord.ItemAmount != null);
        var hasCreditRecords = creditRecordsCount == fileSpec.CashLetter.Bundles.Count;

        var cashLetterControlItemCount = fileSpec.CashLetter.CashLetterControl.CashLetterItemCount.ToInt();
        var checkItemsCount = fileSpec.CashLetter.Bundles.Sum(b => b.CheckItems.Count);
        if (cashLetterControlItemCount != checkItemsCount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains {checkItemsCount} items, but record says there should be {cashLetterControlItemCount} items.";
            validationResult.ErrorMessages.Add(errorMessage);
        }

        var cashLetterControlImageViewCount =
            fileSpec.CashLetter.CashLetterControl.CashLetterImageViewCount.ToInt();
        var checkItemsImageCount = fileSpec.CashLetter.Bundles.Sum(b => b.CheckItems.Sum(a => a.ImageViews.Count));
        if (cashLetterControlImageViewCount != checkItemsImageCount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains {checkItemsImageCount} images, but record says there should be {cashLetterControlImageViewCount} images.";
            if (hasCreditRecords)
            {
                errorMessage =
                    $"Cash letter control record for cash letter is possibly incorrect due to existence of Credit Records. The cash letter contains {checkItemsImageCount} images, but record says there should be {cashLetterControlImageViewCount} images.";
            }

            validationResult.ErrorMessages.Add(errorMessage);
        }


        var cashLetterControlTotalAmount = fileSpec.CashLetter.CashLetterControl.CashLetterTotalAmount.ToDecimal();
        var checkItemsTotalAmount =
            fileSpec.CashLetter.Bundles.Sum(b => b.CheckItems.Sum(c => c.CheckDetail.ItemAmount.ToDecimal()));
        if (cashLetterControlTotalAmount != checkItemsTotalAmount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains items with a total value of ${checkItemsTotalAmount}, but record says the total amount should be ${cashLetterControlTotalAmount}.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
    }

    private static void ValidateFileControl(X9File file, ref ValidationResult validationResult)
    {
        var fileControlTotalItemCount = file.FileControl.TotalItemCount.ToInt();
        var checkItemsCount = file.CashLetter.Bundles.Sum(b => b.CheckItems.Count);
        var checkItemsTotalAmount =
            file.CashLetter.Bundles.Sum(b => b.CheckItems.Sum(c => c.CheckDetail.ItemAmount.ToDecimal()));

        if (fileControlTotalItemCount != checkItemsCount)
        {
            var errorMessage =
                $"File control record is incorrect. The file contains {checkItemsCount} items, but record says there should be {fileControlTotalItemCount} items.";
            validationResult.ErrorMessages.Add(errorMessage);
        }

        var fileControlTotalAmount = file.FileControl.FileTotalAmount.ToDecimal();
        if (fileControlTotalAmount != checkItemsTotalAmount)
        {
            var errorMessage =
                $"File control record is incorrect. The file contains items with a total value of ${checkItemsTotalAmount}, but record says the total amount should be ${fileControlTotalAmount}.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
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

    public static void TestValidate(FileInfo file)
    {
        Console.WriteLine($"Validating {file.Name}...");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using Stream x9File = File.OpenRead(file.FullName);
        using X9Reader reader = new X9Reader(x9File);
        var doc = reader.ReadX9Document();

        Console.WriteLine($"Validation completed...");
    }
}

public class ValidationResult
{
    public bool Status { get; set; }
    public List<string> ErrorMessages { get; set; } = new();

    public ValidationResult(bool status)
    {
        Status = status;
    }
}