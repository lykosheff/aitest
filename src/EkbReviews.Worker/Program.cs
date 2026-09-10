using EkbReviews.Application;
using EkbReviews.Infrastructure;
using EkbReviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IReviewAnalyzer, HeuristicReviewAnalyzer>();
builder.Services.AddScoped<IReviewRepository, PostgresReviewRepository>();

builder.Services.AddDbContext<EkbReviewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EkbReviews")));

builder.Services.AddHttpClient<TwoGisCatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://catalog.api.2gis.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>()
        .GetSection(TwoGisOptions.SectionName)
        .Get<TwoGisOptions>() ?? new TwoGisOptions());

builder.Services.AddHostedService<CatalogWorker>();

await builder.Build().RunAsync();

internal sealed class CatalogWorker(
    ILogger<CatalogWorker> logger,
    TwoGisCatalogClient client,
    TwoGisOptions options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogWarning("2GIS API key is not configured. Set TwoGis:ApiKey to enable catalog discovery.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var places = await client.SearchAsync(options, stoppingToken);

                var organizations = places
                    .Select(x => new Organization(x.Id, x.Name, x.AddressName))
                    .ToArray();

                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();
                await repository.UpsertOrganizationsAsync(organizations, stoppingToken);

                logger.LogInformation(
                    "2GIS returned {Count} organizations for query {Query} and persisted them.",
                    organizations.Length,
                    options.Query);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "2GIS catalog synchronization failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
