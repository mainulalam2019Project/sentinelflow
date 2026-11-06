using Microsoft.EntityFrameworkCore;
using SentinelFlow.Infrastructure;
using SentinelFlow.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg => cfg.WriteTo.Console().Enrich.FromLogContext());
builder.Services.AddDbContext<SentinelFlowDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));
builder.Services.AddSingleton<SimulatorState>();

builder.Services.AddSingleton<IMessageBus>(_ => new RabbitMqBus(
    builder.Configuration["RabbitMq:Host"] ?? "localhost",
    builder.Configuration["RabbitMq:User"] ?? "guest",
    builder.Configuration["RabbitMq:Password"] ?? "guest"));
builder.Services.AddHttpClient("healthcheck", c => c.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddHostedService<HealthCheckWorker>();
builder.Services.AddHostedService<AnomalyDetectionWorker>();
builder.Services.AddHostedService<IncidentPipelineWorker>();

var host = builder.Build();
host.Run();
