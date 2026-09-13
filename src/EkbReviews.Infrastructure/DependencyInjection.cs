using EkbReviews.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EkbReviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEkbReviewsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        if (!Uri.TryCreate(aiOptions.BaseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("Ai:BaseUrl must be a valid absolute URI.");

        services.AddSingleton(aiOptions);
        services.AddHttpClient<OllamaReviewAnalyzer>(client =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds);
        });

        services.AddSingleton<IAiReviewAnalyzer>(sp =>
        {
            if (!string.Equals(aiOptions.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsupported AI provider: {aiOptions.Provider}");

            return sp.GetRequiredService<OllamaReviewAnalyzer>();
        });

        // Регистрация клиентов для 2GIS
        services.AddHttpClient<TwoGisCatalogClient>(client =>
        {
            client.BaseAddress = new Uri("https://catalog.api.2gis.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<TwoGisReviewClient>(client =>
        {
            client.BaseAddress = new Uri("https://catalog.api.2gis.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Регистрация клиента для Яндекс Карт
        services.AddHttpClient<YandexMapsReviewClient>(client =>
        {
            client.BaseAddress = new Uri("https://yandex.ru/sprav/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "EkbReviewsBot/1.0");
        });

        return services;
    }
}
