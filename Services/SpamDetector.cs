using System.Text.RegularExpressions;

namespace StudentPartTime.Services;

public static class SpamDetector
{
    private static readonly Regex PhoneRegex = new(
        @"(?:0|\+84)[\s.\-]?(?:3|5|7|8|9)(?:[\s.\-]?\d){8}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SocialLinkRegex = new(
        @"(?:https?://)?(?:www\.)?(?:zalo\.me|zaloapp\.com|facebook\.com|fb\.com|m\.me|messenger\.com|instagram\.com)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex KeywordRegex = new(
        @"\b(?:zalo|sđt|sdt|add\s*zalo|liên\s*hệ\s+ngoài|facebook|fb)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsSuspicious(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        return PhoneRegex.IsMatch(content)
            || SocialLinkRegex.IsMatch(content)
            || KeywordRegex.IsMatch(content);
    }
}
