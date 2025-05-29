using System;
using System.Text.RegularExpressions;

public static class GenderResolver
{
    public static int? ResolveGender(string? description, string? title = null)
    {
        string combined = ((description ?? "") + " " + (title ?? "")).ToLower();

        if (string.IsNullOrWhiteSpace(combined))
        {
            return null;
        }

        if (Regex.IsMatch(combined, @"\b(девочка|meitene|female|сука|girl|женский|she)\b"))
            return 2;

        if (Regex.IsMatch(combined, @"\b(мальчик|puika|male|кобель|boy|мужской|he)\b"))
            return 1;

        return null;
    }
}
