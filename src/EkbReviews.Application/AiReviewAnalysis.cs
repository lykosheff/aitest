using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed record AiReviewAnalysis(
    bool Interesting,
    int Score,
    bool Conflict,
    bool Humor,
    bool Surprise,
    bool StrongDialogue,
    string StoryType,
    string Title,
    string Reason);

public interface IAiReviewAnalyzer
{
    Task<AiReviewAnalysis> AnalyzeAsync(
        Review review,
        CancellationToken cancellationToken = default);
}
