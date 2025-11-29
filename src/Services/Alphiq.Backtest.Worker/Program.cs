using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// TODO: Add backtest worker services
// builder.Services.AddHostedService<BacktestWorkerService>();

var host = builder.Build();
host.Run();
