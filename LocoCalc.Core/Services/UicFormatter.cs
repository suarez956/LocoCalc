namespace LocoCalc.Services;

public static class UicFormatter
{
    /// <summary>
    /// Formats raw digits to UIC vehicle number format.
    /// Format A: XX XX XXXX XXX-X  (classic locos, 2+2+4+3+1)
    /// Format B: XX XX X XXX XXX-X (newer traction, 2+2+1+3+3+1)
    /// Accepts 0–12 digits; partial input is formatted progressively.
    /// </summary>
    public static string Format(string digits, string format = "A")
    {
        if (string.IsNullOrEmpty(digits)) return string.Empty;

        var count = Math.Min(digits.Length, 12);
        var sb = new System.Text.StringBuilder(18);

        if (format == "B")
        {
            // XX XX X XXX XXX-X — separators before positions 2,4,5,8,11(dash)
            for (int i = 0; i < count; i++)
            {
                if (i == 2 || i == 4 || i == 5 || i == 8) sb.Append(' ');
                else if (i == 11) sb.Append('-');
                sb.Append(digits[i]);
            }
        }
        else
        {
            // XX XX XXXX XXX-X — separators before positions 2,4,8,11(dash)
            for (int i = 0; i < count; i++)
            {
                if (i == 2 || i == 4 || i == 8) sb.Append(' ');
                else if (i == 11) sb.Append('-');
                sb.Append(digits[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>Strips all non-digit characters, returning at most 12 digits.</summary>
    public static string StripToDigits(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return new string(text.Where(char.IsDigit).Take(12).ToArray());
    }

    /// <summary>True when <paramref name="digits"/> represents a complete 12-digit UIC number.</summary>
    public static bool IsComplete(string digits) => digits.Length == 12;

    /// <summary>
    /// Calculates the UIC check digit from the first 11 raw digits.
    /// Odd positions (1-based) × 1, even positions × 2; products ≥ 10 have their digits summed.
    /// Result = (10 − sum mod 10) mod 10.
    /// </summary>
    public static int CalculateCheckDigit(string elevenDigits)
    {
        string substring = elevenDigits.Substring(5, 6);
        int sum = 0;
        for (int i = 0; i < 6; i++)
        {
            int d = substring[i] - '0';
            int product = (i % 2 == 0) ? d : d * 2;   // relative position: even=×1, odd=×2
            if (product >= 10) product = product / 10 + product % 10;
            sum += product;
        }
        return (10 - sum % 10) % 10;
    }

    /// <summary>True when the 12th digit matches the check digit computed from the first 11.</summary>
    public static bool IsCheckDigitValid(string twelveDigits) =>
        twelveDigits.Length == 12 &&
        CalculateCheckDigit(twelveDigits) == (twelveDigits[11] - '0');

    /// <summary>
    /// Checks whether <paramref name="digits"/> is consistent with any of the allowed <paramref name="prefixes"/>
    /// starting at <paramref name="offset"/> in the raw digit string.
    /// Returns <c>null</c> when there are no constraints or not enough digits have been typed yet.
    /// Returns <c>true</c> when at least one prefix is fully matched.
    /// Returns <c>false</c> when every prefix is definitively ruled out.
    /// </summary>
    public static bool? MatchesPrefixes(string digits, IReadOnlyList<string>? prefixes, int offset = 4)
    {
        if (prefixes is null || prefixes.Count == 0) return null;
        if (digits.Length <= offset) return null;

        var tail = digits.Substring(offset);
        bool anyStillPossible = false;

        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrEmpty(prefix)) continue;
            int checkLen = Math.Min(tail.Length, prefix.Length);
            if (string.Compare(tail, 0, prefix, 0, checkLen, StringComparison.Ordinal) == 0)
            {
                if (tail.Length >= prefix.Length) return true; // full match confirmed
                anyStillPossible = true;                       // partial — still viable
            }
        }

        return anyStillPossible ? null : false;
    }
}
