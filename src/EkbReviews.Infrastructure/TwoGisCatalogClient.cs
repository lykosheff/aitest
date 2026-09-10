using System.Text.Json;
using System.Text.Json.Serialization;

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

    private sealed class TwoGisResponse
    {
        public TwoGisResult? Result { get; init; }
    }

    private sealed class TwoGisResult
    {
        public List<TwoGisPlace>? Items { get; init; }
    }
}

public sealed record TwoGisPlace(
    string Id,
    string Name,
    [property: JsonPropertyName("address_name")] string? AddressName,
    string? Type);
