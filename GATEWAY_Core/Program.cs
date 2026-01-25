using Core.Logging;
using Core.Services;
using Grpc.Net.Client;
using network;
using systeminfo;


var builder = WebApplication.CreateBuilder(args);

// Logging: add file logger (configurable via Logging:FilePath env/appsettings, default logs/grpc-server.log)
var logPath = builder.Configuration["Logging:FilePath"] ?? "logs/core/grpc-server.log";
builder.Logging.AddProvider(new FileLoggerProvider(logPath, LogLevel.Information));

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddScoped<NetworkServiceImpl>();
builder.Services.AddScoped<SystemServiceImpl>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<NetworkServiceImpl>();
app.MapGrpcService<SystemServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await app.RunAsync();
