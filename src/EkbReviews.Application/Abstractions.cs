using EkbReviews.Domain;

namespace EkbReviews.Application;

public interface IReviewSource
{
    ReviewSource Source { get; }

    Task<IReadOnlyList<Review>> GetNewReviewsAsync(
        Organization organization,
        CancellationToken cancellationToken = default);
}

public sealed record Organization(
    string Id,
    string Name,
    string? Address = null);

public interface IReviewAnalyzer
{
    Task<ReviewCandidate?> AnalyzeAsync(
        Review review,
        CancellationToken cancellationToken = default);
}

public interface IAiReviewAnalyzer
{
    Task<ReviewCandidate?> AnalyzeAsync(
        Review review,
        CancellationToken cancellationToken = default);
}

public interface IReviewPublisher
{
    Task PublishAsync(
        ReviewCandidate candidate,
        CancellationToken cancellationToken = default);
}
