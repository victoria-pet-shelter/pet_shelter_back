using System;
using System.Text.RegularExpressions;

public static class GenderResolver
{
    public static int ResolveGender(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("⚠️ No gender-related text found.");
            return 1;
        }

        var lower = text.ToLower();
        Console.WriteLine($"[DEBUG] Checking gender text: {lower}");

        if (Regex.IsMatch(lower, @"\b(девочка|meitene|female|сука|girl|женский|she)\b"))
            return 2;

        if (Regex.IsMatch(lower, @"\b(мальчик|puika|male|кобель|boy|мужской|he)\b"))
            return 1;

        Console.WriteLine("⚠️ Gender could not be resolved. Defaulting to male.");
        return 1;
    }
}
