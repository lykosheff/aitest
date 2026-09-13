using System.Text.Json;
using System.Text.Json.Serialization;
using EkbReviews.Domain;

namespace EkbReviews.Infrastructure;

public sealed class TwoGisCatalogClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TwoGisPlace>> SearchAsync(
        TwoGisOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("TwoGis:ApiKey is not configured.");

        var query = new List<string>
        {
            $"q={Uri.EscapeDataString(options.Query)}",
            "type=branch",
            "has_reviews=true",
            "sort=rating",
            $"page_size={Math.Clamp(options.PageSize, 1, 50)}",
            $"key={Uri.EscapeDataString(options.ApiKey)}",
            "locale=ru_RU"
        };

        using var response = await httpClient.GetAsync(
            $"3.0/items?{string.Join('&', query)}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<TwoGisResponse>(stream, JsonOptions, cancellationToken);

        return result?.Result?.Items ?? [];
    }
}

public sealed class TwoGisReviewClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Review>> GetReviewsAsync(
        string organizationId,
        string organizationName,
        string? organizationAddress,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TwoGis:ApiKey is not configured.");

        var reviews = new List<Review>();
        int page = 1;
        const int pageSize = 50;

        while (true)
        {
            var query = new List<string>
            {
                $"fil_id={Uri.EscapeDataString(organizationId)}",
                $"page={page}",
                $"page_size={pageSize}",
                $"key={Uri.EscapeDataString(apiKey)}",
                "locale=ru_RU"
            };

            using var response = await httpClient.GetAsync(
                $"3.0/reviews?{string.Join('&', query)}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                break;

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<TwoGisReviewsResponse>(stream, JsonOptions, cancellationToken);

            if (result?.Result?.Items is null || result.Result.Items.Count == 0)
                break;

            foreach (var reviewDto in result.Result.Items)
            {
                var replies = reviewDto.Replies?.Select(r => new ReviewReply(
                    r.AuthorName ?? "Организация",
                    r.Text ?? "",
                    r.PublishedAt.HasValue 
                        ? DateTimeOffset.FromUnixTimeSeconds(r.PublishedAt.Value) 
                        : DateTimeOffset.UtcNow
                )).ToArray() ?? [];

                var review = new Review(
                    Id: reviewDto.Id.ToString(),
                    Source: ReviewSource.TwoGis,
                    OrganizationId: organizationId,
                    OrganizationName: organizationName,
                    OrganizationAddress: organizationAddress,
                    Rating: reviewDto.RatingValue,
                    AuthorName: reviewDto.AuthorName ?? "Аноним",
                    PublishedAt: reviewDto.PublishedAt.HasValue 
                        ? DateTimeOffset.FromUnixTimeSeconds(reviewDto.PublishedAt.Value) 
                        : DateTimeOffset.UtcNow,
                    Text: reviewDto.Text ?? "",
                    Photos: reviewDto.Photos?.Select(p => new ReviewPhoto(p.Url ?? "")).ToArray() ?? [],
                    SourceUrl: $"https://2gis.ru/review/{reviewDto.Id}",
                    Replies: replies,
                    CollectedAt: DateTimeOffset.UtcNow
                );

                reviews.Add(review);
            }

            if (result.Result.Items.Count < pageSize)
                break;

            page++;
            
            // Ограничиваем количество страниц для предотвращения чрезмерных запросов
            if (page > 10)
                break;
        }

        return reviews;
    }

    private sealed class TwoGisReviewsResponse
    {
        public TwoGisReviewsResult? Result { get; init; }
    }

    private sealed class TwoGisReviewsResult
    {
        public List<TwoGisReviewDto>? Items { get; init; }
    }

    private sealed class TwoGisReviewDto
    {
        public long Id { get; init; }
        public int RatingValue { get; init; }
        public string? AuthorName { get; init; }
        public long? PublishedAt { get; init; }
        public string? Text { get; init; }
        public List<TwoGisPhotoDto>? Photos { get; init; }
        public List<TwoGisReplyDto>? Replies { get; init; }
    }

    private sealed class TwoGisPhotoDto
    {
        public string? Url { get; init; }
    }

    private sealed class TwoGisReplyDto
    {
        public string? AuthorName { get; init; }
        public string? Text { get; init; }
        public long? PublishedAt { get; init; }
    }
}

public sealed record TwoGisPlace(
    string Id,
    string Name,
    [property: JsonPropertyName("address_name")] string? AddressName,
    string? Type);
