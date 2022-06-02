public class CashLetterModel
{
    public int BundleSequence { get; set; }
    public string CreditAccount { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public int CheckRecordsCount { get; set; }
    public int CheckImagesCount { get; set; }
    public decimal CheckTotal { get; set; }
}