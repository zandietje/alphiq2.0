using Alphiq.Backtest.Worker;
using Alphiq.Infrastructure.Messaging.DependencyInjection;
using Alphiq.Infrastructure.Supabase.DependencyInjection;
using Alphiq.Infrastructure.Supabase.Repositories;
using Alphiq.TradingEngine.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add NATS messaging with Aspire
builder.AddNatsClient("nats");
builder.Services.AddNatsMessaging();

// Add Supabase candle repository
builder.Services.AddSupabaseCandleRepository(builder.Configuration);

// Register strategy factory
builder.Services.AddSingleton<IStrategyFactory, StrategyFactory>();

// Register backtest orchestrator
builder.Services.AddScoped<BacktestOrchestrator>();

// Register backtest worker service
builder.Services.AddHostedService<BacktestWorkerService>();

var host = builder.Build();
host.Run();
