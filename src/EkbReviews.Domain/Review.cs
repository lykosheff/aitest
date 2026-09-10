namespace EkbReviews.Domain;

public enum ReviewSource
{
    TwoGis,
    YandexMaps
}

public sealed record ReviewPhoto(string Url);

public sealed record ReviewReply(
    string AuthorName,
    string Text,
    DateTimeOffset PublishedAt);

public sealed record Review(
    string Id,
    ReviewSource Source,
    string OrganizationId,
    string OrganizationName,
    string? OrganizationAddress,
    int Rating,
    string AuthorName,
    DateTimeOffset PublishedAt,
    string Text,
    IReadOnlyList<ReviewPhoto> Photos,
    string? SourceUrl,
    IReadOnlyList<ReviewReply> Replies,
    DateTimeOffset CollectedAt);

public sealed record ReviewCandidate(
    Review Review,
    int Score,
    string? Title,
    string Reason,
    bool HasDialogue,
    bool HasConflict,
    bool HasHumor,
    bool HasSurprise);
