namespace EkbReviews.Infrastructure;

public sealed class OpenAiReviewAnalyzerOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-5.6-luna";
    public string Endpoint { get; init; } = "https://api.openai.com/v1/responses";
}
