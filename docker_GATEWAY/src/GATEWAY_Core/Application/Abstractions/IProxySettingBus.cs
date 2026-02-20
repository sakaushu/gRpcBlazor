using GATEWAYCore.Domain.Models;

namespace GATEWAYCore.Application.Abstractions
{
    public interface IProxySettingBus
    {
        Task SetSystemdEnvironmentViaDBus(ProxyConfig proxyConfig);
        Task<ProxyConfig> GetProxySettingsAsync();
    }
}
