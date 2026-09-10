using EkbReviews.Domain;
using NUnit.Framework;

namespace EkbReviews.Application;

[TestFixture]
public sealed class HeuristicReviewAnalyzerTests
{
    private readonly HeuristicReviewAnalyzer _analyzer = new();

    [Test]
    public async Task ShortReview_IsIgnored()
    {
        var review = CreateReview("Вкусно и хорошо.");

        var result = await _analyzer.AnalyzeAsync(review);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DialogueWithConflict_GetsHighScore()
    {
        var review = CreateReview(
            "Ждали заказ почти час. Еда приехала холодной, позвали администратора. Это какой-то кошмар!",
            new ReviewReply("Ресторан", "Нам очень жаль, но это не обман и не причина для скандала. Мы разберёмся.", DateTimeOffset.UtcNow));

        var result = await _analyzer.AnalyzeAsync(review);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Score, Is.GreaterThanOrEqualTo(80));
            Assert.That(result.HasDialogue, Is.True);
            Assert.That(result.HasConflict, Is.True);
        });
    }

    [Test]
    public async Task OrdinaryLongReview_DoesNotBecomeCandidateWithoutStrongSignals()
    {
        var review = CreateReview(
            "Очень приятное место. Красивый интерьер, хорошая музыка, официант быстро принёс блюда. Паста вкусная, кофе тоже понравился, обязательно зайдём ещё раз.");

        var result = await _analyzer.AnalyzeAsync(review);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FiveStarHumorousReview_IsCandidate()
    {
        var review = CreateReview(
            "Пять звёзд за то, что официант принёс десерт раньше кофе! 😂 Сначала подумал, что это случайность, но потом понял — это новый уровень сервиса!");

        var result = await _analyzer.AnalyzeAsync(review);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.HasHumor, Is.True);
            Assert.That(result.Score, Is.GreaterThanOrEqualTo(45));
        });
    }

    private static Review CreateReview(string text, ReviewReply[]? replies = null, int rating = 3) =>
        new(
            Guid.NewGuid().ToString("N"),
            ReviewSource.TwoGis,
            "org-1",
            "Тестовый ресторан",
            "Екатеринбург",
            rating,
            "Автор",
            DateTimeOffset.UtcNow,
            text,
            [],
            null,
            replies ?? [],
            DateTimeOffset.UtcNow);
}
