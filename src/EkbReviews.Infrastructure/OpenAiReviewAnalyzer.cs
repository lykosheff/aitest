using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EkbReviews.Application;
using EkbReviews.Domain;

namespace EkbReviews.Infrastructure;

public sealed class OpenAiReviewAnalyzer(
    HttpClient httpClient,
    OpenAiReviewAnalyzerOptions options) : IAiReviewAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiReviewAnalysis> AnalyzeAsync(
        Review review,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var conversation = new List<object>
        {
            new { author = review.AuthorName, role = "customer", text = review.Text }
        };

        conversation.AddRange(review.Replies.Select(reply => new
        {
            author = reply.AuthorName,
            role = "business",
            text = reply.Text
        }));

        var request = new
        {
            model = options.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "Ты редактор Telegram-канала с интересными отзывами ресторанов Екатеринбурга. Оцени, стоит ли публиковать отзыв или диалог. Ищи реальные истории, конфликт, юмор, неожиданные повороты, конкретику и сильную переписку. Не считай интересным обычное 'вкусно/не вкусно'. Верни только JSON по заданной схеме."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        organization = review.OrganizationName,
                        rating = review.Rating,
                        publishedAt = review.PublishedAt,
                        conversation
                    }, JsonOptions)
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "review_analysis",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            interesting = new { type = "boolean" },
                            score = new { type = "integer", minimum = 0, maximum = 100 },
                            conflict = new { type = "boolean" },
                            humor = new { type = "boolean" },
                            surprise = new { type = "boolean" },
                            strongDialogue = new { type = "boolean" },
                            storyType = new { type = "string" },
                            title = new { type = "string" },
                            reason = new { type = "string" }
                        },
                        required = new[]
                        {
                            "interesting", "score", "conflict", "humor", "surprise",
                            "strongDialogue", "storyType", "title", "reason"
                        }
                    }
                }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        requestMessage.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = ExtractOutputText(responseBody);
        var result = JsonSerializer.Deserialize<AiReviewAnalysis>(json, JsonOptions);

        return result ?? throw new InvalidOperationException("OpenAI returned an empty review analysis.");
    }

    private static string ExtractOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("output", out var output))
            throw new InvalidOperationException("OpenAI response does not contain output.");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                    return text.GetString() ?? throw new InvalidOperationException("OpenAI returned empty output text.");
            }
        }

        throw new InvalidOperationException("OpenAI response does not contain output text.");
    }
}
