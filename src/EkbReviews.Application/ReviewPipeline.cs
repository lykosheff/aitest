using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed record ReviewPipelineResult(
    int Processed,
    int Candidates,
    IReadOnlyList<ReviewCandidate> RankedCandidates);

public sealed class ReviewPipeline(IReviewAnalyzer analyzer)
{
    public async Task<ReviewPipelineResult> ProcessAsync(
        IEnumerable<Review> reviews,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<ReviewCandidate>();
        var processed = 0;

        foreach (var review in reviews)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processed++;

            var candidate = await analyzer.AnalyzeAsync(review, cancellationToken);
            if (candidate is not null)
                candidates.Add(candidate);
        }

        var ranked = candidates
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Review.PublishedAt)
            .ToArray();

        return new ReviewPipelineResult(processed, ranked.Length, ranked);
    }
}
