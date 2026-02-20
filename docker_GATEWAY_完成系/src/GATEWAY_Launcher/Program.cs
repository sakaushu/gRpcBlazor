using GATEWAY_Launcher.Components;
using GATEWAY_Launcher.Components.Models;
using GATEWAY_Launcher.Components.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var grpcServerAddress = builder.Configuration["GrpcServer:Address"] ?? "http://localhost:5000";
builder.Services.AddGrpcClient<GATEWAYCore.NetworkConfigService.NetworkConfigServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});
builder.Services.AddGrpcClient<GATEWAYCore.TimeConfigService.TimeConfigServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});
builder.Services.AddGrpcClient<GATEWAYCore.ConfigsService.ConfigsServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});
builder.Services.AddGrpcClient<GATEWAYCore.HTTPProxyConfigService.HTTPProxyConfigServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});
builder.Services.AddGrpcClient<GATEWAYCore.AzIotEdgeConfigService.AzIotEdgeConfigServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});
builder.Services.AddGrpcClient<GATEWAYCore.PortForwardConfigService.PortForwardConfigServiceClient>(o =>
{
    o.Address = new Uri(grpcServerAddress);
});

builder.Services.AddTransient<NicConfigService>();
builder.Services.AddTransient<NicConfigModel>();
builder.Services.AddTransient<ProxyService>();
builder.Services.AddTransient<ProxyModel>();
builder.Services.AddTransient<TimeSettingService>();
builder.Services.AddTransient<TimeSettingModel>();
builder.Services.AddTransient<AzureIoTEdgeService>();
builder.Services.AddTransient<AzureIoTEdgeModel>();
builder.Services.AddTransient<ConnectionSettingsModel>();
builder.Services.AddTransient<PortForwardingModel>();
builder.Services.AddTransient<PortForwardingService>();
builder.Services.AddScoped<HardwareInfoModel>();
builder.Services.AddScoped<HardwareInfoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
