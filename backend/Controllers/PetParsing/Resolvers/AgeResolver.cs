using System.Text.RegularExpressions;
using System;

public static class AgeResolver
{
    public static float? ParseAge(string? ageText)
    {
        if (string.IsNullOrWhiteSpace(ageText))
        {
            // Console.WriteLine("⚠️ ageText is empty or null.");
            return null;
        }

        ageText = ageText.ToLower().Trim();
        // Console.WriteLine($"[DEBUG] Parsing ageText: {ageText}");

        try
        {
            float totalMonths = 0;

            // "2 gadi", "3 mēn.", "4.5 года"
            var matches = Regex.Matches(
                ageText,
                @"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>gadi|gads|gadus|год(?:а|ов)?|лет|года|years?|yr|y|mēneš[iu\.]*|mēn\.?|men(?:es[iu])?|месяц(?:а|ев)?|мес(?:яц[аев]*)?|months?|m)",
                RegexOptions.IgnoreCase
            );

            foreach (Match match in matches)
            {
                var numberPart = match.Groups["value"].Value.Replace(',', '.');
                var unit = match.Groups["unit"].Value.ToLowerInvariant();

                if (!float.TryParse(numberPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float number))
                    continue;

                // months
                if (unit.Contains("mēn") || unit.Contains("men") || unit.Contains("мес") || unit.Contains("месяц") || unit.StartsWith("month") || unit == "m")
                {
                    totalMonths += number;
                }
                // years
                else if (unit.Contains("gad") || unit.Contains("год") || unit.Contains("лет") || unit.Contains("year") || unit.StartsWith("yr") || unit == "y")
                {
                    totalMonths += number * 12;
                }
            }

            if (totalMonths == 0)
            {
                Console.WriteLine($"⚠️ Could not parse ageText: '{ageText}'");
            }

            return totalMonths;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse age: '{ageText}' | {ex.Message}");
            return 0;
        }
    }
}
