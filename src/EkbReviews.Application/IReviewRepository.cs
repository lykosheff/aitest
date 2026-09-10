using EkbReviews.Domain;

namespace EkbReviews.Application;

public interface IReviewRepository
{
    Task UpsertOrganizationsAsync(
        IReadOnlyList<Organization> organizations,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsReviewAsync(
        ReviewSource source,
        string reviewId,
        CancellationToken cancellationToken = default);

    Task SaveReviewAsync(
        Review review,
        CancellationToken cancellationToken = default);
}
