namespace EkbReviews.Infrastructure;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; init; } = "Ollama";
    public string Model { get; init; } = "gpt-oss";
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string? ApiKey { get; init; }
    public double Temperature { get; init; } = 0;
    public int TimeoutSeconds { get; init; } = 120;
}
