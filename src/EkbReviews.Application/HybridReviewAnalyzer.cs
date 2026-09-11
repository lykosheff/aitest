using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed class HybridReviewAnalyzer(
    HeuristicReviewAnalyzer heuristic,
    IAiReviewAnalyzer ai) : IReviewAnalyzer
{
    public async Task<ReviewCandidate?> AnalyzeAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        var heuristicCandidate = await heuristic.AnalyzeAsync(review, cancellationToken);
        if (heuristicCandidate is null)
            return null;

        var analysis = await ai.AnalyzeAsync(review, cancellationToken);
        if (!analysis.Interesting || analysis.Score < 45)
            return null;

        return new ReviewCandidate(
            review,
            Math.Clamp(analysis.Score, 0, 100),
            string.IsNullOrWhiteSpace(analysis.Title) ? heuristicCandidate.Title : analysis.Title.Trim(),
            string.IsNullOrWhiteSpace(analysis.Reason) ? heuristicCandidate.Reason : analysis.Reason.Trim(),
            analysis.StrongDialogue,
            analysis.Conflict,
            analysis.Humor,
            analysis.Surprise);
    }
}
