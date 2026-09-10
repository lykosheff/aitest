namespace EkbReviews.Infrastructure;

public sealed class TwoGisOptions
{
    public const string SectionName = "TwoGis";

    public string ApiKey { get; init; } = string.Empty;
    public string Query { get; init; } = "ресторан Екатеринбург";
    public int PageSize { get; init; } = 50;
}
