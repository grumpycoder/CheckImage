using System.Globalization;
using Org.BouncyCastle.Utilities;

public static class Helpers
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }
    public static int ToInt(this string @this)
    {
        return Convert.ToInt32(@this);
    }

    public static string ToMoney(this decimal @this)
    {
        return Math.Round(@this, 2).ToString("C");
    }

    public static decimal ToDecimal(this string @this)
    {
        var wholeNumber = @this.Substring(0, @this.Length - 2);
        var decimalNumber = @this.Substring(@this.Length - 2);
        var n = $"{wholeNumber}.{decimalNumber}"; 
        return Convert.ToDecimal(n);
    }

    public static string ToDate(this string @this)
    {
        DateTime d = DateTime.ParseExact(@this, "yyyyMMdd", CultureInfo.InvariantCulture);
        return d.ToShortDateString(); 
        
        var isValidDate = DateTime.TryParse(@this, out var result);
        return isValidDate ? result.ToShortDateString() : DateTime.MinValue.Date.ToShortDateString(); 
    }

    public static string To24HourTime(this string @this)
    {
        var hour = @this.Substring(0, 2);
        var minute = @this.Substring(2, 2);
        var time = new TimeOnly(hour.ToInt(), minute.ToInt()); 
        return time.ToString("HH:mm"); 
    }
    
    public static string To12HourTime(this string @this)
    {
        var hour = @this.Substring(0, 2);
        var minute = @this.Substring(2, 2);
        var time = new TimeOnly(hour.ToInt(), minute.ToInt()); 
        return time.ToString("HH:mm"); 
    }
}