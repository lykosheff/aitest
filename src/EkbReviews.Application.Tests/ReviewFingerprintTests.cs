using EkbReviews.Domain;
using NUnit.Framework;

namespace EkbReviews.Application.Tests;

[TestFixture]
public sealed class ReviewFingerprintTests
{
    [Test]
    public void SameContentWithDifferentWhitespace_HasSameFingerprint()
    {
        var first = Create("Очень вкусно!   Обязательно придём ещё раз.");
        var second = Create("очень   вкусно! обязательно придём ещё раз.");

        Assert.That(ReviewFingerprint.Create(first), Is.EqualTo(ReviewFingerprint.Create(second)));
    }

    [Test]
    public void DifferentReplyText_HasDifferentFingerprint()
    {
        var first = Create("Еда холодная.", new ReviewReply("Ресторан", "Разберёмся.", DateTimeOffset.UtcNow));
        var second = Create("Еда холодная.", new ReviewReply("Ресторан", "Напишите нам.", DateTimeOffset.UtcNow));

        Assert.That(ReviewFingerprint.Create(first), Is.Not.EqualTo(ReviewFingerprint.Create(second)));
    }

    private static Review Create(string text, ReviewReply? reply = null) =>
        new(Guid.NewGuid().ToString("N"), ReviewSource.TwoGis, "org-1", "Ресторан", null, 3,
            "Автор", DateTimeOffset.UtcNow, text, [], null,
            reply is null ? [] : [reply], DateTimeOffset.UtcNow);
}
