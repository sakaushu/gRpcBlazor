using GATEWAYCore.Domain.Models;
using Tmds.DBus;

namespace GATEWAYCore.Application.Abstractions;
///<summary>
/// NetworkManager操作インタフェース
/// </summary>
public interface INetworkManagerPort
{
    Task<List<NicConfigModel>> ResolveDevicePathsAsync(List<NicConfigModel> nicConfigs, CancellationToken ct);
    Task<List<NicConfigModel>> ResolveActiveConnection(List<NicConfigModel> nicConfigs, CancellationToken ct);
    Task UpdateSettingsAsync(NicConfigModel nicConfig, bool isUplink, string defaultGateway, DnsConfigModel Dns, CancellationToken ct);
    Task<List<NicConfigModel>> ListNicConfigsAsync(CancellationToken ct);
    Task<(List<string> dnsServers, string? gateway)> GetDnsAndGatewayAsync(CancellationToken ct);
    Task<List<NicConfigModel>> GetIpv4Configs(List<NicConfigModel> nicConfigs, CancellationToken ct);
    NetworkConfigModel LoadNetworkConfigsFromConfigFile();

}
