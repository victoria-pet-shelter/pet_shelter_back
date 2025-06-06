using System.Text.RegularExpressions;
using System;

public static class PriceResolver
{
    public static string? ExtractPrice(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        try
        {
            var match = Regex.Match(description, @"(\d[\d\s]*[\.,]?\d*)\s*€");
            if (match.Success)
            {
                string raw = match.Groups[1].Value;
                raw = raw.Replace(" ", "").Replace(",", ".");
                return raw;
            }
            else
            {
                // Console.WriteLine("[DEBUG] Сena not in description.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error with cena: {ex.Message}");
        }

        return null;

    }
}