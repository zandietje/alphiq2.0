var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var nats = builder.AddNats("nats")
    .WithJetStream();

var redis = builder.AddRedis("redis");

// Services
var tradeNode = builder.AddProject<Projects.Alphiq_TradeNode>("tradenode")
    .WithReference(nats)
    .WithReference(redis);

var backtestWorker = builder.AddProject<Projects.Alphiq_Backtest_Worker>("backtest-worker")
    .WithReference(nats)
    .WithReference(redis);

var optimizer = builder.AddProject<Projects.Alphiq_Optimizer_Service>("optimizer")
    .WithReference(nats)
    .WithReference(redis);

var apiGateway = builder.AddProject<Projects.Alphiq_Api_Gateway>("api-gateway")
    .WithReference(nats)
    .WithReference(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
