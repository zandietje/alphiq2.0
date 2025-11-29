using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// TODO: Add optimizer services

var host = builder.Build();
host.Run();
