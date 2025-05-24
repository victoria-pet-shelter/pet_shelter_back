private float ParseAge(string? ageText)
    {
        if (string.IsNullOrWhiteSpace(ageText))
        {
            Console.WriteLine("⚠️ ageText is empty or null.");
            return 0;
        }

        ageText = ageText.ToLower().Trim();
        Console.WriteLine($"[DEBUG] Parsing ageText: {ageText}");

        try
        {
            var match = Regex.Match(
                ageText,
                @"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>gadi|gads|gadus|мес(?:яц[аев]*)?|месяц(?:а|ев)?|год(?:а|ов)?|лет|года|months?|years?|mēneš[iu]?|men|yr|yrs|y|m)",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
            {
                Console.WriteLine($"⚠️ Could not parse ageText: '{ageText}'");
                return 0;
            }

            var numberPart = match.Groups["value"].Value.Replace(',', '.');
            var unit = match.Groups["unit"].Value;

            float number = float.Parse(numberPart, System.Globalization.CultureInfo.InvariantCulture);

            if (unit.StartsWith("mēn") || unit.StartsWith("мес") || unit.StartsWith("men") || unit.StartsWith("m") || unit.StartsWith("month"))
            {
                return number;
            }
            else if (unit.StartsWith("gad") || unit.StartsWith("год") || unit.StartsWith("лет") || unit.StartsWith("yr") || unit.StartsWith("y") || unit.StartsWith("year"))
            {
                return number * 12;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse age: '{ageText}' | {ex.Message}");
        }

        return 0;
    }