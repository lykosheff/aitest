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
        if (analysis is null)
            return null;

        var score = Math.Clamp((heuristicCandidate.Score + analysis.Score) / 2, 0, 100);
        var reason = string.IsNullOrWhiteSpace(analysis.Reason)
            ? heuristicCandidate.Reason
            : analysis.Reason;

        return heuristicCandidate with
        {
            Score = score,
            Title = string.IsNullOrWhiteSpace(analysis.Title) ? heuristicCandidate.Title : analysis.Title,
            Reason = reason,
            HasDialogue = heuristicCandidate.HasDialogue || analysis.HasDialogue,
            HasConflict = heuristicCandidate.HasConflict || analysis.HasConflict,
            HasHumor = heuristicCandidate.HasHumor || analysis.HasHumor,
            HasSurprise = heuristicCandidate.HasSurprise || analysis.HasSurprise
        };
    }
}
