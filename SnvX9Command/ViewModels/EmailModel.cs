namespace SnvX9Command;

public class EmailModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileHeaderDate { get; set; }
    public string? FileHeaderTime { get; set; }
    public IList<CashLetterModel> CashLetters { get; set; } = new List<CashLetterModel>();
}