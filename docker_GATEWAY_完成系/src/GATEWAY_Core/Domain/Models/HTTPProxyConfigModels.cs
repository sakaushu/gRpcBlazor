using System.Reflection.Metadata;
using GATEWAYCore;

namespace GATEWAYCore.Domain.Models
{
    public sealed record ProxyConfig(
        HTTPProxyConfig HttpProxy,
        HTTPSProxyConfig HttpsProxy
    );

    public sealed record HTTPProxyConfig(
        bool IsEnabled = false,
        string ProxyAddress = "",
        int ProxyPort = 0,
        string Username = "",
        string Password = ""
    )
    {
        public string ToProxyUrl(bool withCredentials = false)
        {
            return withCredentials && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password)
                ? $"http://{Username}:{Password}@{ProxyAddress}:{ProxyPort}"
                : $"http://{ProxyAddress}:{ProxyPort}";
        }

        public string ToConfigFormatProxyAddress()
        {
            return $"{ProxyAddress}:{ProxyPort}";
        }

        public bool isValid()
        {
            return IsEnabled && !string.IsNullOrEmpty(ProxyAddress) && ProxyPort > 0;
        }

        public static HTTPProxyConfig FromHTTPProxySetting(HTTPProxySetting? setting)
        {
            return (setting == null) ? new HTTPProxyConfig() : new HTTPProxyConfig(
                setting.Enabled,
                setting.ProxyAddress,
                setting.ProxyPort,
                setting.Username,
                setting.Password
            );
        }
        public static HTTPProxyConfig FromConfigFile(string proxyEnable, string proxyAddress)
        {
            bool isEnabled = proxyEnable == GATEWAYCore.Infrastructure.Configs.Constants.StrEnabled;
            if (string.IsNullOrEmpty(proxyAddress) || !isEnabled)
            {
                return new HTTPProxyConfig();
            }
            var parts = proxyAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            {
                return new HTTPProxyConfig();
            }
            return new HTTPProxyConfig(
                IsEnabled: true,
                ProxyAddress: parts[0],
                ProxyPort: port
            );
        }
    }

    public sealed record HTTPSProxyConfig(
        bool IsEnabled = false,
        string ProxyAddress = "",
        int ProxyPort = 0,
        string Username = "",
        string Password = ""
    )
    {
        public string ToProxyUrl(bool withCredentials = false)
        {
            return withCredentials && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password)
                ? $"http://{Username}:{Password}@{ProxyAddress}:{ProxyPort}"
                : $"http://{ProxyAddress}:{ProxyPort}";
        }
        public bool isValid()
        {
            return IsEnabled && !string.IsNullOrEmpty(ProxyAddress) && ProxyPort > 0;
        }

        public static HTTPSProxyConfig FromHTTPSProxySetting(HTTPSProxySetting? setting)
        {
            return (setting == null) ? new HTTPSProxyConfig() : new HTTPSProxyConfig(
                setting.Enabled,
                setting.ProxyAddress,
                setting.ProxyPort,
                setting.Username,
                setting.Password
            );
        }

        public static HTTPSProxyConfig FromConfigFile(string proxyEnable, string proxyAddress)
        {
            bool isEnabled = proxyEnable == "1";
            if (string.IsNullOrEmpty(proxyAddress) || !isEnabled)
            {
                return new HTTPSProxyConfig();
            }
            var parts = proxyAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            {
                return new HTTPSProxyConfig();
            }
            return new HTTPSProxyConfig(
                IsEnabled: true,
                ProxyAddress: parts[0],
                ProxyPort: port
            );
        }
    }
}
