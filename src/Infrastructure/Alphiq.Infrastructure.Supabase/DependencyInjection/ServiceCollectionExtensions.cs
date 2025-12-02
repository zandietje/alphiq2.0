using System.Net.Http.Headers;
using Alphiq.Configuration.Abstractions;
using Alphiq.Infrastructure.Supabase.Options;
using Alphiq.Infrastructure.Supabase.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alphiq.Infrastructure.Supabase.DependencyInjection;

/// <summary>
/// Extension methods for registering Supabase services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Supabase strategy configuration services.
    /// </summary>
    public static IServiceCollection AddSupabaseStrategyConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));

        // Register HttpClient with Supabase headers
        services.AddHttpClient<IStrategyRepository, StrategyRepository>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;

            client.BaseAddress = new Uri(options.Url.TrimEnd('/') + "/");

            // CRITICAL: Supabase requires BOTH headers for authentication
            client.DefaultRequestHeaders.Add("apikey", options.ApiKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

        // Register provider
        services.AddScoped<IStrategyConfigProvider, SupabaseStrategyConfigProvider>();

        return services;
    }
}
