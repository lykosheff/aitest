using EkbReviews.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IReviewAnalyzer, HeuristicReviewAnalyzer>();
builder.Services.AddHostedService<ReviewWorker>();

await builder.Build().RunAsync();

internal sealed class ReviewWorker(
    ILogger<ReviewWorker> logger,
    IReviewAnalyzer analyzer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EkbReviews worker started. Source adapters are not configured yet.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // The real pipeline will be: collect -> deduplicate -> analyze -> rank -> publish.
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
