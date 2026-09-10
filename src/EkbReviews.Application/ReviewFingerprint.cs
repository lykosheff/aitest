using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EkbReviews.Domain;

namespace EkbReviews.Application;

public static partial class ReviewFingerprint
{
    public static string Create(Review review)
    {
        var parts = new List<string>
        {
            review.Source.ToString(),
            review.OrganizationId,
            Normalize(review.Text)
        };

        parts.AddRange(review.Replies
            .OrderBy(x => x.PublishedAt)
            .Select(x => $"{Normalize(x.AuthorName)}:{Normalize(x.Text)}"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        var normalized = Whitespace().Replace(value.Trim().ToLowerInvariant(), " ");
        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
