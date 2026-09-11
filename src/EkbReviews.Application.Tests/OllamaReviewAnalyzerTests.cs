using System.Net;
using System.Text;
using System.Text.Json;
using EkbReviews.Domain;
using EkbReviews.Infrastructure;
using NUnit.Framework;

namespace EkbReviews.Application.Tests;

[TestFixture]
public sealed class OllamaReviewAnalyzerTests
{
    [Test]
    public async Task AnalyzeAsync_SendsStructuredChatRequest_AndParsesAnalysis()
    {
        string? requestJson = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestJson = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"message\":{\"content\":\"{\\\"interesting\\\":true,\\\"score\\\":87,\\\"conflict\\\":true,\\\"humor\\\":false,\\\"surprise\\\":true,\\\"strongDialogue\\\":true,\\\"storyType\\\":\\\"конфликт\\\",\\\"title\\\":\\\"Неожиданный ответ ресторана\\\",\\\"reason\\\":\\\"Сильный диалог и конфликт\\\"}\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var analyzer = new OllamaReviewAnalyzer(httpClient, new AiOptions());

        var result = await analyzer.AnalyzeAsync(CreateReview());

        Assert.Multiple(() =>
        {
            Assert.That(result.Interesting, Is.True);
            Assert.That(result.Score, Is.EqualTo(87));
            Assert.That(result.Conflict, Is.True);
            Assert.That(result.Surprise, Is.True);
            Assert.That(result.StrongDialogue, Is.True);
            Assert.That(result.StoryType, Is.EqualTo("конфликт"));
            Assert.That(result.Title, Is.EqualTo("Неожиданный ответ ресторана"));
        });

        Assert.That(requestJson, Is.Not.Null);
        using var requestDocument = JsonDocument.Parse(requestJson!);
        var root = requestDocument.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("model").GetString(), Is.EqualTo("gpt-oss"));
            Assert.That(root.GetProperty("stream").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("format").ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(root.GetProperty("options").GetProperty("temperature").GetDouble(), Is.EqualTo(0));
        });

        Assert.That(handler.RequestUri?.AbsolutePath, Is.EqualTo("/api/chat"));
    }

    private static Review CreateReview() => new(
        "review-1", ReviewSource.TwoGis, "org-1", "Тестовый ресторан", "Екатеринбург",
        2, "Автор", DateTimeOffset.UtcNow,
        "Ждали заказ почти час, после чего позвали администратора и начали выяснять, что произошло.",
        [], null,
        [new ReviewReply("Ресторан", "Разберёмся с ситуацией.", DateTimeOffset.UtcNow)],
        DateTimeOffset.UtcNow);

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(responder(request));
        }
    }
}
