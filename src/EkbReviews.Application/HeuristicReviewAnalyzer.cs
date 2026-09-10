using System.Text.RegularExpressions;
using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed class HeuristicReviewAnalyzer : IReviewAnalyzer
{
    private static readonly string[] ConflictWords = ["ужас", "кошмар", "хам", "обман", "верните", "жалоб", "скандал", "разочарован"];
    private static readonly string[] HumorWords = ["смешно", "😂", "🤣", "шут", "прикол", "ирони", "мем"];
    private static readonly string[] SurpriseWords = ["не ожидал", "не ожидала", "впервые", "никогда", "оказалось", "в итоге", "вдруг"];

    public Task<ReviewCandidate?> AnalyzeAsync(Review review, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = Normalize(review.Text);
        if (text.Length < 40)
            return Task.FromResult<ReviewCandidate?>(null);

        var conversation = new ReviewConversationExtractor().Extract(review);
        var allText = string.Join(" ", conversation.Messages.Select(x => x.Text));
        var hasDialogue = conversation.IsDialogue;
        var hasConflict = ContainsAny(allText, ConflictWords);
        var hasHumor = ContainsAny(allText, HumorWords);
        var hasSurprise = text.Contains('!') && text.Contains('?') || ContainsAny(text, SurpriseWords);
        var specificity = CalculateSpecificity(text);
        var freshness = CalculateFreshness(review.PublishedAt);

        var score = 10;
        score += Math.Min(15, text.Length / 100);
        score += hasDialogue ? Math.Min(25, 10 + review.Replies.Count * 5) : 0;
        score += hasConflict ? 15 : 0;
        score += hasHumor ? 15 : 0;
        score += hasSurprise ? 10 : 0;
        score += specificity;
        score += freshness;
        score += review.Rating is 1 or 5 ? 5 : 0;
        score = Math.Min(100, score);

        if (score < 45)
            return Task.FromResult<ReviewCandidate?>(null);

        var reasons = new List<string>();
        if (hasDialogue) reasons.Add("диалог с заведением");
        if (hasConflict) reasons.Add("конфликт");
        if (hasHumor) reasons.Add("юмор");
        if (hasSurprise) reasons.Add("неожиданный поворот");
        if (specificity >= 8) reasons.Add("много конкретики");
        if (freshness >= 8) reasons.Add("свежий отзыв");

        var title = CreateTitle(text);
        return Task.FromResult<ReviewCandidate?>(new ReviewCandidate(
            review,
            score,
            title,
            reasons.Count == 0 ? "необычный отзыв" : string.Join(", ", reasons),
            hasDialogue,
            hasConflict,
            hasHumor,
            hasSurprise));
    }

    private static int CalculateSpecificity(string text)
    {
        var signals = new[] { "час", "минут", "руб", "заказ", "официант", "администратор", "блюд", "счёт", "чек", "доставка" };
        return Math.Min(10, signals.Count(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)) * 2);
    }

    private static int CalculateFreshness(DateTimeOffset publishedAt)
    {
        var age = DateTimeOffset.UtcNow - publishedAt;
        if (age <= TimeSpan.FromHours(24)) return 10;
        if (age <= TimeSpan.FromDays(3)) return 8;
        if (age <= TimeSpan.FromDays(7)) return 5;
        if (age <= TimeSpan.FromDays(14)) return 2;
        return 0;
    }

    private static string CreateTitle(string text)
    {
        var title = Regex.Replace(text, @"\s+", " ").Trim();
        return title.Length <= 90 ? title : title[..90] + "…";
    }

    private static string Normalize(string text) => Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();

    private static bool ContainsAny(string text, IEnumerable<string> words) =>
        words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
}
