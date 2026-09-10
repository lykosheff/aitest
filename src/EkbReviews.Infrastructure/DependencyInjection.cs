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

        return services;
    }
}
