using Alphiq.Brokers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alphiq.Infrastructure.Broker.Simulated.DependencyInjection;

/// <summary>
/// Extension methods for registering simulated broker services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the simulated broker adapters for backtesting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration for BacktestSettings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimulatedBroker(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Bind settings from configuration if provided
        if (configuration != null)
        {
            services.Configure<BacktestSettings>(configuration.GetSection("Backtest"));
        }

        // Register BacktestClock as singleton (shared across all adapters)
        services.AddSingleton<BacktestClock>();
        services.AddSingleton<IClock>(sp => sp.GetRequiredService<BacktestClock>());

        // Register BacktestSettings
        services.AddSingleton(sp =>
        {
            var config = configuration?.GetSection("Backtest");
            if (config?.Exists() == true)
            {
                return config.Get<BacktestSettings>() ?? new BacktestSettings();
            }
            return new BacktestSettings();
        });

        // Register SimulatedOrderExecution as singleton
        services.AddSingleton<SimulatedOrderExecution>();
        services.AddSingleton<IOrderExecution>(sp => sp.GetRequiredService<SimulatedOrderExecution>());

        // Register BacktestMarketDataFeed as singleton
        services.AddSingleton<BacktestMarketDataFeed>();
        services.AddSingleton<IMarketDataFeed>(sp => sp.GetRequiredService<BacktestMarketDataFeed>());

        return services;
    }
}
