using GATEWAYCore.Domain.Models;

namespace GATEWAYCore.Application.Abstractions;

/// <summary>
/// systemd サービスを操作するクライアントのインターフェース
/// </summary>
public interface ISystemdClient
{
    /// <summary>
    /// サービスを起動
    /// </summary>
    /// <param name="serviceName">サービス名（例: "saferun@eth0-tcp-80-192.168.1.1-8080.service"）</param>
    /// <param name="ct">キャンセルトークン</param>
    Task StartServiceAsync(string serviceName, CancellationToken ct);

    /// <summary>
    /// 複数のポートフォワードルールを一括適用
    /// </summary>
    Task ApplyPortForwardRulesAsync(
        IEnumerable<PortForwardRuleModel> rules,
        CancellationToken ct);
}
