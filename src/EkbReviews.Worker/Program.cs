using EkbReviews.Application;
using EkbReviews.Infrastructure;
using EkbReviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<HeuristicReviewAnalyzer>();
builder.Services.AddEkbReviewsInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IReviewAnalyzer, HybridReviewAnalyzer>();
builder.Services.AddScoped<IReviewRepository, PostgresReviewRepository>();

builder.Services.AddDbContext<EkbReviewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EkbReviews")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConfiguration>()
        .GetSection(TwoGisOptions.SectionName)
        .Get<TwoGisOptions>() ?? new TwoGisOptions());

builder.Services.AddHostedService<ReviewCollectionWorker>();

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EkbReviewsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();

internal sealed class ReviewCollectionWorker(
    ILogger<ReviewCollectionWorker> logger,
    TwoGisCatalogClient twoGisCatalogClient,
    TwoGisReviewClient twoGisReviewClient,
    YandexMapsReviewClient yandexMapsReviewClient,
    TwoGisOptions options,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogWarning("2GIS API key is not configured. Set TwoGis:ApiKey to enable catalog discovery.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Получаем организации из 2GIS
                var places = Array.Empty<TwoGisPlace>();
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    places = await twoGisCatalogClient.SearchAsync(options, stoppingToken);
                    
                    var organizations = places
                        .Select(x => new Organization(x.Id, x.Name, x.AddressName))
                        .ToArray();

                    await using var scope = serviceProvider.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();
                    await repository.UpsertOrganizationsAsync(organizations, stoppingToken);

                    logger.LogInformation(
                        "2GIS returned {Count} organizations for query {Query} and persisted them.",
                        organizations.Length,
                        options.Query);
                }

                // Собираем отзывы из 2GIS для найденных организаций
                foreach (var place in places)
                {
                    try
                    {
                        var reviews = await twoGisReviewClient.GetReviewsAsync(
                            place.Id,
                            place.Name,
                            place.AddressName,
                            options.ApiKey,
                            stoppingToken);

                        await using var scope = serviceProvider.CreateAsyncScope();
                        var repository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();

                        foreach (var review in reviews)
                        {
                            if (!await repository.ExistsReviewAsync(review.Source, review.Id, stoppingToken))
                            {
                                await repository.SaveReviewAsync(review, stoppingToken);
                            }
                        }

                        logger.LogInformation(
                            "Collected {Count} reviews from 2GIS for {Organization}",
                            reviews.Count,
                            place.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to collect reviews from 2GIS for {Organization}", place.Name);
                    }
                }

                // Собираем отзывы из Яндекс Карт (если есть организации в БД)
                await using (var scope = serviceProvider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<EkbReviewsDbContext>();
                    var organizations = await dbContext.Organizations.ToListAsync(stoppingToken);

                    foreach (var org in organizations)
                    {
                        try
                        {
                            var yandexReviews = await yandexMapsReviewClient.GetReviewsAsync(
                                org.Id,
                                org.Name,
                                org.Address,
                                stoppingToken);

                            var repository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();

                            foreach (var review in yandexReviews)
                            {
                                if (!await repository.ExistsReviewAsync(review.Source, review.Id, stoppingToken))
                                {
                                    await repository.SaveReviewAsync(review, stoppingToken);
                                }
                            }

                            logger.LogInformation(
                                "Collected {Count} reviews from Yandex Maps for {Organization}",
                                yandexReviews.Count,
                                org.Name);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to collect reviews from Yandex Maps for {Organization}", org.Name);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Review collection failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
