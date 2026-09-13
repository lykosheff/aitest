using System.Text.Json;
using System.Text.Json.Serialization;
using EkbReviews.Domain;

namespace EkbReviews.Infrastructure;

public sealed class YandexMapsReviewClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Получает отзывы из Яндекс Карт для организации.
    /// Примечание: Яндекс не предоставляет официального публичного API для отзывов,
    /// поэтому этот метод использует неофициальный endpoint.
    /// Для продакшена рекомендуется использовать Яндекс.Business API.
    /// </summary>
    public async Task<IReadOnlyList<Review>> GetReviewsAsync(
        string organizationId,
        string organizationName,
        string? organizationAddress,
        CancellationToken cancellationToken = default)
    {
        var reviews = new List<Review>();
        
        // Используем неофициальный API Яндекс Карт
        // В продакшене следует использовать официальный Яндекс.Business API
        var url = $"https://yandex.ru/sprav/api/v1/orgs/{organizationId}/reviews";
        
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return reviews;

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<YandexReviewsResponse>(stream, JsonOptions, cancellationToken);

            if (result?.Reviews is null)
                return reviews;

            foreach (var reviewDto in result.Reviews)
            {
                var replies = reviewDto.Responses?.Select(r => new ReviewReply(
                    r.Author?.Name ?? "Организация",
                    r.Text ?? "",
                    !string.IsNullOrEmpty(r.CreatedAt) 
                        ? DateTimeOffset.Parse(r.CreatedAt) 
                        : DateTimeOffset.UtcNow
                )).ToArray() ?? [];

                var review = new Review(
                    Id: reviewDto.Id?.ToString() ?? Guid.NewGuid().ToString(),
                    Source: ReviewSource.YandexMaps,
                    OrganizationId: organizationId,
                    OrganizationName: organizationName,
                    OrganizationAddress: organizationAddress,
                    Rating: reviewDto.Rating?.Value ?? 0,
                    AuthorName: reviewDto.Author?.Name ?? "Аноним",
                    PublishedAt: !string.IsNullOrEmpty(reviewDto.CreatedAt) 
                        ? DateTimeOffset.Parse(reviewDto.CreatedAt) 
                        : DateTimeOffset.UtcNow,
                    Text: reviewDto.Text ?? "",
                    Photos: reviewDto.Images?.Select(img => new ReviewPhoto(img.Url ?? "")).ToArray() ?? [],
                    SourceUrl: $"https://yandex.ru/maps/org/{organizationId}/review/{reviewDto.Id}",
                    Replies: replies,
                    CollectedAt: DateTimeOffset.UtcNow
                );

                reviews.Add(review);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Организация не найдена или отзывы недоступны
            return reviews;
        }
        catch (Exception)
        {
            // При ошибках парсинга или недоступности API возвращаем пустой список
            // В продакшене следует добавить логирование
            return reviews;
        }

        return reviews;
    }

    /// <summary>
    /// Альтернативный метод получения отзывов через поиск организаций.
    /// </summary>
    public async Task<IReadOnlyList<Organization>> SearchOrganizationsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var organizations = new List<Organization>();
        
        try
        {
            var url = $"https://yandex.ru/sprav/api/v1/search?text={Uri.EscapeDataString(query)}";
            
            using var response = await httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return organizations;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<YandexSearchResponse>(stream, JsonOptions, cancellationToken);

            if (result?.Organizations is null)
                return organizations;

            foreach (var orgDto in result.Organizations)
            {
                var org = new Organization(
                    Id: orgDto.Id?.ToString() ?? Guid.NewGuid().ToString(),
                    Name: orgDto.Name ?? "Неизвестно",
                    Address: orgDto.Address
                );
                
                organizations.Add(org);
            }
        }
        catch
        {
            // При ошибках возвращаем пустой список
            return organizations;
        }

        return organizations;
    }

    private sealed class YandexReviewsResponse
    {
        public List<YandexReviewDto>? Reviews { get; init; }
    }

    private sealed class YandexReviewDto
    {
        public long? Id { get; init; }
        public YandexRatingDto? Rating { get; init; }
        public YandexAuthorDto? Author { get; init; }
        public string? CreatedAt { get; init; }
        public string? Text { get; init; }
        public List<YandexImageDto>? Images { get; init; }
        public List<YandexResponseDto>? Responses { get; init; }
    }

    private sealed class YandexRatingDto
    {
        public int Value { get; init; }
    }

    private sealed class YandexAuthorDto
    {
        public string? Name { get; init; }
    }

    private sealed class YandexImageDto
    {
        public string? Url { get; init; }
    }

    private sealed class YandexResponseDto
    {
        public YandexAuthorDto? Author { get; init; }
        public string? Text { get; init; }
        public string? CreatedAt { get; init; }
    }

    private sealed class YandexSearchResponse
    {
        public List<YandexOrganizationDto>? Organizations { get; init; }
    }

    private sealed class YandexOrganizationDto
    {
        public long? Id { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
    }
}
