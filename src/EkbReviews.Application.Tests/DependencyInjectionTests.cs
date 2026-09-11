using EkbReviews.Application;
using EkbReviews.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EkbReviews.Application.Tests;

[TestFixture]
public sealed class DependencyInjectionTests
{
    [Test]
    public void ReviewAnalyzer_ResolvesHeuristicHybridAndOllamaAiAnalyzer()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddSingleton<HeuristicReviewAnalyzer>();
        services.AddEkbReviewsInfrastructure(configuration);
        services.AddSingleton<IReviewAnalyzer, HybridReviewAnalyzer>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var analyzer = provider.GetRequiredService<IReviewAnalyzer>();
        var aiAnalyzer = provider.GetRequiredService<IAiReviewAnalyzer>();

        Assert.Multiple(() =>
        {
            Assert.That(analyzer, Is.TypeOf<HybridReviewAnalyzer>());
            Assert.That(aiAnalyzer, Is.TypeOf<OllamaReviewAnalyzer>());
        });
    }
}
