using System.Net.Http.Json;
using System.Text.Json;
using EkbReviews.Application;
using EkbReviews.Domain;

namespace EkbReviews.Infrastructure;

public sealed class OllamaReviewAnalyzer(
    HttpClient httpClient,
    AiOptions options) : IReviewAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly object ResponseSchema = new
    {
        type = "object",
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
        required = new[] { "interesting", "score", "conflict", "humor", "surprise", "strongDialogue", "storyType", "title", "reason" }
    };

    public async Task<ReviewCandidate?> AnalyzeAsync(Review review, CancellationToken cancellationToken = default)
    {
        var conversation = new ReviewConversationExtractor().Extract(review);
        var conversationText = string.Join("\n\n", conversation.Messages.Select(FormatMessage));

        var request = new
        {
            model = options.Model,
            stream = false,
            format = ResponseSchema,
            options = new { temperature = options.Temperature },
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "Ты редактор Telegram-канала о заведениях Екатеринбурга. Отбирай действительно интересные отзывы и диалоги. Не придумывай факты. Интересны конфликты, неожиданные ситуации, юмор, странные случаи, сильные ответы заведений и содержательные диалоги. Обычный отзыв 'вкусно, красиво, придём ещё' должен получать низкую оценку. Верни только JSON по заданной схеме."
                },
                new
                {
                    role = "user",
                    content = $"Оцени отзыв и диалог. Заведение: {review.OrganizationName}. Оценка: {review.Rating}/5.\n\n{conversationText}"
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync("/api/chat", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        var result = JsonSerializer.Deserialize<AiReviewResult>(payload.Message.Content, JsonOptions)
            ?? throw new InvalidOperationException("Ollama returned invalid structured output.");

        if (!result.Interesting || result.Score < 45)
            return null;

        return new ReviewCandidate(
            review,
            Math.Clamp(result.Score, 0, 100),
            string.IsNullOrWhiteSpace(result.Title) ? null : result.Title.Trim(),
            result.Reason.Trim(),
            result.StrongDialogue,
            result.Conflict,
            result.Humor,
            result.Surprise);
    }

    private static string FormatMessage(ConversationMessage message) =>
        $"[{message.AuthorType}] {message.AuthorName}: {message.Text}";

    private sealed record OllamaResponse(OllamaMessage Message);
    private sealed record OllamaMessage(string Content);

    private sealed record AiReviewResult(
        bool Interesting,
        int Score,
        bool Conflict,
        bool Humor,
        bool Surprise,
        bool StrongDialogue,
        string StoryType,
        string Title,
        string Reason);
}
