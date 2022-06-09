using System.Text;
using CompAnalytics.X9;
using X9;

public static class FileService
{
    public static ValidationResult Validate(FileInfo file)
    {
        Console.WriteLine($"Validating {file.Name}...");
        var processor = new Processor();
        processor.Execute(new FileStream(file.FullName, FileMode.Open));

        var check = processor.FileSpec;

        var validationResult = new ValidationResult(true);

        var cashLetterControlItemCount = check.CashLetter.CashLetterControl.CashLetterItemCount.ToInt(); 
        var checkItemsCount = check.CashLetter.Bundles.Sum(b => b.CheckItems.Count);
        if (cashLetterControlItemCount != checkItemsCount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains {checkItemsCount} items, but record says there should be {cashLetterControlItemCount} items.";     
            validationResult.ErrorMessages.Add(errorMessage);
        }

        var cashLetterControlImageViewCount = check.CashLetter.CashLetterControl.CashLetterImageViewCount.ToInt();
        var checkItemsImageCount = check.CashLetter.Bundles.Sum(b => b.CheckItems.Sum(a => a.ImageViews.Count));
        if (cashLetterControlImageViewCount != checkItemsImageCount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains {checkItemsImageCount} images, but record says there should be {cashLetterControlImageViewCount} images.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
        

        var cashLetterControlTotalAmount = check.CashLetter.CashLetterControl.CashLetterTotalAmount.ToDecimal();
        var checkItemsTotalAmount = check.CashLetter.Bundles.Sum(b => b.CheckItems.Sum(c => c.CheckDetail.ItemAmount.ToDecimal()));
        if (cashLetterControlTotalAmount != checkItemsTotalAmount)
        {
            var errorMessage =
                $"Cash letter control record for cash letter is incorrect. The cash letter contains items with a total value of ${checkItemsTotalAmount}, but record says the total amount should be ${cashLetterControlTotalAmount}.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
        

        var fileControlTotalItemCount = check.FileControl.TotalItemCount.ToInt();
        if (fileControlTotalItemCount != checkItemsCount)
        {
            var errorMessage =
                $"File control record is incorrect. The file contains {checkItemsCount} items, but record says there should be {fileControlTotalItemCount} items.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
        

        var fileControlTotalAmount = check.FileControl.FileTotalAmount.ToInt();
        if (fileControlTotalAmount != checkItemsTotalAmount)
        {
            var errorMessage =
                $"File control record is incorrect. The file contains items with a total value of ${checkItemsTotalAmount}, but record says the total amount should be ${fileControlTotalAmount}.";
            validationResult.ErrorMessages.Add(errorMessage);
        }
        

        // var fileControlTotalRecordCount = check.FileControl.TotalRecordCount.ToInt(); 
        // var e6 =
        //     "File control record is incorrect. The file contains 1030 records, but record says there should be 1022 records."; 
        
        foreach (var bundle in check.CashLetter.Bundles)
        {
            var bundleId = bundle.BundleHeader.BundleId;

            var bundleControlItemCount = bundle.BundleControl.BundleItemCount.ToInt();
            var bundleCheckItemsCount = bundle.CheckItems.Count; 
            if (bundleControlItemCount != bundleCheckItemsCount)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is incorrect. The bundle contains {bundleCheckItemsCount}, but record says there should be {bundleControlItemCount} items.";
                validationResult.ErrorMessages.Add(errorMessage);
            }

            var bundleControlImagesWithinBundleCount = bundle.BundleControl.ImagesWithinBundleCount.ToInt();
            var bundleCheckItemImageViewCount = bundle.CheckItems.Sum(ci => ci.ImageViews.Count); 
            if(bundleControlImagesWithinBundleCount != bundleCheckItemImageViewCount)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is incorrect. The bundle contains {bundleCheckItemImageViewCount}, but record says there should be {bundleControlImagesWithinBundleCount} images.";
                validationResult.ErrorMessages.Add(errorMessage);    
            }
            
            var bundleControlTotalAmount = bundle.BundleControl.BundleTotalAmount.ToDecimal();
            var bundleCheckItemsTotalAmount = bundle.CheckItems.Sum(checkItem => checkItem.CheckDetail.ItemAmount.ToDecimal());
            if (bundleControlTotalAmount != bundleCheckItemsTotalAmount)
            {
                var errorMessage =
                    $"Bundle control record for bundle {bundleId} is incorrect.  The bundle contains items with a total value of ${bundleCheckItemsTotalAmount}, but record says the total amount should be ${bundleControlTotalAmount}.";
                validationResult.ErrorMessages.Add(errorMessage);  
            }
        }

        Console.WriteLine($"Validation completed...");
        return validationResult;
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

public class ValidationResult
{
    public bool Status { get; private set; }
    public List<string> ErrorMessages { get; set; } = new();

    public ValidationResult(bool status)
    {
        Status = status;
    }
}