namespace GATEWAYCore.Domain.Models;

/// <summary>
/// ネットワークインターフェース設定
/// </summary>
public record NetworkInterfaceConfigs(
    string Ip,
    string Mask,
    string Dhcp
);

/// <summary>
/// DNS設定
/// </summary>
public record DnsConfigs(
    string Primary,
    string Secondary
);

/// <summary>
/// その他のネットワーク設定
/// </summary>
public record OtherNetworkConfigs(
    string ComEthEndDisable,
    string ComEthWaitEnable,
    string ComEthWaitChange
);

/// <summary>
/// ネットワーク全体の設定
/// </summary>
public class NetworkConfigs : IDisposable
{
    /// <summary>
    /// ネットワークインターフェース設定の配列（3つのネットワーク）
    /// </summary>
    public NetworkInterfaceConfigs[] Networks { get; }
    
    public DnsConfigs Dns { get; }
    public OtherNetworkConfigs OtherConfigs { get; }
    public string Gateway { get; }

    public NetworkConfigs(
        NetworkInterfaceConfigs[] networks,
        DnsConfigs dns,
        OtherNetworkConfigs otherConfigs,
        string gateway)
    {
        Networks = networks;
        Dns = dns;
        OtherConfigs = otherConfigs;
        Gateway = gateway;
    }

    /// <summary>
    /// リソースをクリーンアップ
    /// </summary>
    public void Dispose()
    {
        // ネットワーク設定のクリーンアップ処理
        // 現在は特に管理するリソースはないが、将来の拡張に対応
    }
}
