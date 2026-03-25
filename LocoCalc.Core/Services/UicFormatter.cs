namespace LocoCalcAvalonia.Services;

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
}
