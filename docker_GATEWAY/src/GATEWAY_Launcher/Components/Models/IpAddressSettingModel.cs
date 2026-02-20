namespace GATEWAY_Launcher.Components.Models
{

    public class EthInfo
    {
        public string? Name { get; set; }
        public string? Mac { get; set; }
        public bool DhcpEnabled { get; set; }
        public string? IpAddress { get; set; }
        public string? SubnetMask { get; set; }
    }
    public class NicConfigModel
    {
        public List<EthInfo> Interfaces { get; set; } = new();
        public string? DefaultGateway { get; set; }
        public List<string?> DnsServers { get; set; } = new();
    }
}
