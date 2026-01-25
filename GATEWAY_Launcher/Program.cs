using GATEWAY_Launcher.Components;
using GATEWAY_Launcher.Logging;
using Grpc.Net.Client;
using network;
using systeminfo;

var builder = WebApplication.CreateBuilder(args);

var logPath = builder.Configuration["Logging:FilePath"] ?? "logs/launcher/launcher.log";
builder.Logging.AddProvider(new FileLoggerProvider(logPath, LogLevel.Information));

// gRPC server address
var grpcServerAddress = builder.Configuration["GrpcServer:Address"] ?? "http://localhost:5053";

// Register gRPC clients
builder.Services.AddGrpcClient<NetworkService.NetworkServiceClient>(options =>
{
    options.Address = new Uri(grpcServerAddress);
});

builder.Services.AddGrpcClient<SystemService.SystemServiceClient>(options =>
{
    options.Address = new Uri(grpcServerAddress);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
