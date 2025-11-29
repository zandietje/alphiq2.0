using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// TODO: Add services
// builder.Services.AddSingleton<TradingEngineService>();

var host = builder.Build();
host.Run();
