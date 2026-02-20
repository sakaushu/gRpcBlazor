using Tmds.DBus;


///<summary>
/// ネットワーク設定（複数NICのIP/DNS設定）に関するDomainモデルクラス
/// </summary>
namespace GATEWAYCore.Domain.Models;

public enum Ipv4MethodModel
{
    Unspecified = 0,
    Dhcp = 1,
    Static = 2
}

public sealed record Ipv4ConfigModel(
    Ipv4MethodModel Method,
    string? Address,
    int? Prefix
);

public sealed record DnsConfigModel(
    List<string> Servers
);

public sealed record NicConfigModel(
    string InterfaceName,
    string MacAddress,
    Ipv4ConfigModel Ipv4,
    ObjectPath devicePath, //画面には出さないD-Bus用のプロパティ（NIC特定用）
    ObjectPath connectionPath, //画面には出さないD-Bus用のプロパティ（接続特定用）
    string connectionId //画面には出さないプロパティ（接続特定用）
);


public sealed record NetworkConfigModel(
    List<NicConfigModel> NicConfigs,
    string DefaultGateway,
    DnsConfigModel Dns
);
