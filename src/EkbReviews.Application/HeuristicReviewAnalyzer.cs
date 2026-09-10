using System.Text.RegularExpressions;
using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed class HeuristicReviewAnalyzer : IReviewAnalyzer
{
    private static readonly string[] ConflictWords = ["ужас", "кошмар", "хам", "обман", "верните", "жалоб", "скандал"];
    private static readonly string[] HumorWords = ["смешно", "😂", "🤣", "шут", "прикол"];

    public Task<ReviewCandidate?> AnalyzeAsync(Review review, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(review.Text) || review.Text.Trim().Length < 40)
            return Task.FromResult<ReviewCandidate?>(null);

        var text = review.Text.Trim();
        var hasDialogue = review.Replies.Count > 0;
        var hasConflict = ContainsAny(text, ConflictWords) || review.Replies.Any(r => ContainsAny(r.Text, ConflictWords));
        var hasHumor = ContainsAny(text, HumorWords) || review.Replies.Any(r => ContainsAny(r.Text, HumorWords));
        var hasSurprise = text.Contains('!') && text.Contains('?');

        var score = 15;
        score += Math.Min(25, text.Length / 80);
        score += hasDialogue ? 25 : 0;
        score += hasConflict ? 15 : 0;
        score += hasHumor ? 10 : 0;
        score += hasSurprise ? 10 : 0;
        score += review.Rating is 1 or 5 ? 5 : 0;
        score = Math.Min(100, score);

        if (score < 45)
            return Task.FromResult<ReviewCandidate?>(null);

        var reason = string.Join(", ", new[]
        {
            hasDialogue ? "есть ответ заведения" : null,
            hasConflict ? "конфликт" : null,
            hasHumor ? "юмор" : null,
            hasSurprise ? "неожиданная формулировка" : null
        }.Where(x => x is not null));

        var title = Regex.Replace(text, @"\s+", " ").Trim();
        if (title.Length > 90)
            title = title[..90] + "…";

        return Task.FromResult<ReviewCandidate?>(new ReviewCandidate(
            review,
            score,
            title,
            string.IsNullOrEmpty(reason) ? "необычный отзыв" : reason,
            hasDialogue,
            hasConflict,
            hasHumor,
            hasSurprise));
    }

    private static bool ContainsAny(string text, IEnumerable<string> words) =>
        words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
}
