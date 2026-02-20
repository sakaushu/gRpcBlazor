using GATEWAYCore.Application;
using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Application.UseCase;
using GATEWAYCore.Infrastructure;
using GATEWAYCore.Infrastructure.Configs;
using GATEWAYCore.Infrastructure.HostedServices;
using GATEWAYCore.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddScoped<INetworkManagerPort, NetworkManagerPort>();
builder.Services.AddScoped<ISystemdTimedateClient, SystemdTimedateClient>();
builder.Services.AddScoped<IProxySettingBus, ProxySettingBus>();
builder.Services.AddScoped<IAzIotEdgeClient, AzIotEdgeClient>();
builder.Services.AddScoped<ISystemdClient, SystemdClient>();
builder.Services.AddScoped<NetworkConfigUseCase>();
builder.Services.AddScoped<TimeConfigUseCase>();
builder.Services.AddScoped<HTTPProxyConfigUseCase>();
builder.Services.AddScoped<AzIotEdgeConfigUseCase>();
builder.Services.AddScoped<PortForwardUseCase>();

// ホステッドサービスを登録
builder.Services.AddHostedService<GatewayStartupService>();

// アプリの実行フォルダ直下の「Configs」ディレクトリを設定のベースパスとして使用
var settingsBasePath = Path.Combine(AppContext.BaseDirectory, "Configs");
builder.Services.AddSingleton(serviceProvider =>
    new ConfigsManager(
        serviceProvider.GetRequiredService<ILogger<ConfigsManager>>(),
        settingsBasePath
    )
);

// GatewayStartupServiceをホストサービスとして登録
// 起動後に実行したい処理がある場合は、GatewayStartupServiceのStartedAsyncメソッドに記述してください。
builder.Services.AddHostedService<GATEWAYCore.Infrastructure.HostedServices.GatewayStartupService>();

var app = builder.Build();

// Initialize ConfigsManager
var settingsManager = app.Services.GetRequiredService<ConfigsManager>();
await settingsManager.InitializeAsync();

app.MapGrpcService<NetworkConfigGrpcService>();
app.MapGrpcService<TimeConfigGrpcService>();
app.MapGrpcService<ConfigsGrpcService>();
app.MapGrpcService<HTTPProxyConfigGrpcService>();
app.MapGrpcService<AzIotEdgeConfigGrpcService>();
app.MapGrpcService<PortForwardGrpcService>();

// Configure the HTTP request pipeline.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
