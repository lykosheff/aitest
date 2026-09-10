using EkbReviews.Domain;
using NUnit.Framework;

namespace EkbReviews.Application;

[TestFixture]
public sealed class ReviewPipelineTests
{
    [Test]
    public async Task ProcessAsync_RanksCandidatesByScore()
    {
        var analyzer = new HeuristicReviewAnalyzer();
        var pipeline = new ReviewPipeline(analyzer);

        var reviews = new[]
        {
            Create("Обычный длинный отзыв о хорошем ресторане. Всё вкусно, красиво и аккуратно, придраться особо не к чему.", 3),
            Create("Это был кошмар! Ждали час, блюдо холодное. Позвали администратора, начался скандал! Почему так?", 1,
                new ReviewReply("Ресторан", "Мы всё исправим, но это не обман. Верните чек, пожалуйста.", DateTimeOffset.UtcNow))
        };

        var result = await pipeline.ProcessAsync(reviews);

        Assert.That(result.Processed, Is.EqualTo(2));
        Assert.That(result.Candidates, Is.EqualTo(1));
        Assert.That(result.RankedCandidates[0].HasDialogue, Is.True);
    }

    private static Review Create(string text, int rating, ReviewReply[]? replies = null) =>
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
