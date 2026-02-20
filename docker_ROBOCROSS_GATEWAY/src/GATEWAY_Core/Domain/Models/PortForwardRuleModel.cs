namespace GATEWAYCore.Domain.Models;

/// <summary>
/// ポートフォワーディングルールのドメインモデル
/// </summary>
public record PortForwardRuleModel(
    string InterfaceName,
    bool IsTcp,
    int SourcePort,
    string DestIp,
    int DestPort
);