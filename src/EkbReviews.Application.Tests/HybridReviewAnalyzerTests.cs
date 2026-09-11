using EkbReviews.Domain;
using NUnit.Framework;

namespace EkbReviews.Application.Tests;

[TestFixture]
public sealed class HybridReviewAnalyzerTests
{
    [Test]
    public async Task HeuristicRejects_AndAiIsNotCalled()
    {
        var ai = new FakeAiReviewAnalyzer();
        var analyzer = new HybridReviewAnalyzer(new HeuristicReviewAnalyzer(), ai);

        var result = await analyzer.AnalyzeAsync(CreateReview("Коротко и вкусно."));

        Assert.That(result, Is.Null);
        Assert.That(ai.Calls, Is.EqualTo(0));
    }

    [Test]
    public async Task AiRejects_ThenCandidateIsDiscarded()
    {
        var ai = new FakeAiReviewAnalyzer(new AiReviewAnalysis(
            false, 90, true, false, false, true, "конфликт", "", "Не стоит публиковать"));
        var analyzer = new HybridReviewAnalyzer(new HeuristicReviewAnalyzer(), ai);

        var result = await analyzer.AnalyzeAsync(CreateInterestingReview());

        Assert.That(result, Is.Null);
        Assert.That(ai.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task AiAccepts_ThenCandidateUsesAiFields()
    {
        var ai = new FakeAiReviewAnalyzer(new AiReviewAnalysis(
            true, 92, true, true, true, true, "история", "Невероятный ответ ресторана", "Сильный диалог"));
        var analyzer = new HybridReviewAnalyzer(new HeuristicReviewAnalyzer(), ai);

        var result = await analyzer.AnalyzeAsync(CreateInterestingReview());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Score, Is.EqualTo(92));
            Assert.That(result.Title, Is.EqualTo("Невероятный ответ ресторана"));
            Assert.That(result.Reason, Is.EqualTo("Сильный диалог"));
            Assert.That(result.HasDialogue, Is.True);
            Assert.That(result.HasConflict, Is.True);
            Assert.That(result.HasHumor, Is.True);
            Assert.That(result.HasSurprise, Is.True);
        });
    }

    private static Review CreateInterestingReview() => new(
        "review-1", ReviewSource.TwoGis, "org-1", "Тестовый ресторан", "Екатеринбург",
        2, "Автор", DateTimeOffset.UtcNow,
        "Ждали заказ почти час. Еда приехала холодной, позвали администратора. Это какой-то кошмар и скандал!",
        [], null,
        [new ReviewReply("Ресторан", "Нам очень жаль, мы разберёмся с ситуацией.", DateTimeOffset.UtcNow)],
        DateTimeOffset.UtcNow);

    private static Review CreateReview(string text) => new(
        "review-1", ReviewSource.TwoGis, "org-1", "Тестовый ресторан", "Екатеринбург",
        3, "Автор", DateTimeOffset.UtcNow, text, [], null, [], DateTimeOffset.UtcNow);

    private sealed class FakeAiReviewAnalyzer(AiReviewAnalysis? analysis = null) : IAiReviewAnalyzer
    {
        public int Calls { get; private set; }

        public Task<AiReviewAnalysis> AnalyzeAsync(
            Review review,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(analysis ?? new AiReviewAnalysis(
                true, 80, false, false, false, false, "история", "Заголовок", "Причина"));
        }
    }
}
