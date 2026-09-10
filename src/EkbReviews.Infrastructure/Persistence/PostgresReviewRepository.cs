using EkbReviews.Application;
using EkbReviews.Domain;
using Microsoft.EntityFrameworkCore;

namespace EkbReviews.Infrastructure.Persistence;

public sealed class PostgresReviewRepository(EkbReviewsDbContext db) : IReviewRepository
{
    public async Task UpsertOrganizationsAsync(
        IReadOnlyList<Organization> organizations,
        CancellationToken cancellationToken = default)
    {
        foreach (var organization in organizations)
        {
            var entity = await db.Organizations.FindAsync([organization.Id], cancellationToken);
            if (entity is null)
            {
                db.Organizations.Add(new OrganizationEntity
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Address = organization.Address
                });
            }
            else
            {
                entity.Name = organization.Name;
                entity.Address = organization.Address;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsReviewAsync(
        ReviewSource source,
        string reviewId,
        CancellationToken cancellationToken = default) =>
        db.Reviews.AnyAsync(x => x.Source == source && x.Id == reviewId, cancellationToken);

    public async Task SaveReviewAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(review.Source, review.Id);

        if (await db.Reviews.AnyAsync(x => x.Key == key, cancellationToken))
            return;

        var entity = new ReviewEntity
        {
            Key = key,
            Id = review.Id,
            Source = review.Source,
            OrganizationId = review.OrganizationId,
            OrganizationName = review.OrganizationName,
            OrganizationAddress = review.OrganizationAddress,
            Rating = review.Rating,
            AuthorName = review.AuthorName,
            PublishedAt = review.PublishedAt,
            Text = review.Text,
            SourceUrl = review.SourceUrl,
            CollectedAt = review.CollectedAt,
            Replies = review.Replies.Select(x => new ReviewReplyEntity
            {
                ReviewKey = key,
                AuthorName = x.AuthorName,
                Text = x.Text,
                PublishedAt = x.PublishedAt
            }).ToList()
        };

        db.Reviews.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildKey(ReviewSource source, string reviewId) => $"{source}:{reviewId}";
}
