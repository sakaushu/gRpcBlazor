using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Infrastructure.Configs;
using System.Text.Json;

namespace GATEWAYCore.Application.UseCase;

/// <summary>
/// ポートフォワーディング設定のユースケース
/// </summary>
public sealed class PortForwardUseCase
{
    private readonly ISystemdClient _systemd;
    private readonly ILogger<PortForwardUseCase> _logger;
    private readonly ConfigsManager _configsManager;
    private readonly INetworkManagerPort _networkManagerPort;

    public PortForwardUseCase(ISystemdClient systemd, ILogger<PortForwardUseCase> logger, ConfigsManager configsManager, INetworkManagerPort networkManagerPort)
    {
        _systemd = systemd;
        _logger = logger;
        _configsManager = configsManager;
        _networkManagerPort = networkManagerPort;
    }

    /// <summary>
    /// ポートフォワーディング設定を適用
    /// </summary>
    /// <param name="rules">適用するポートフォワーディングルールのリスト</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>適用結果のタスク</returns>
    public async Task ApplyPortForwardsAsync(List<PortForwardRuleModel> rules, CancellationToken ct)
    {
        _logger.LogInformation("Applying {Count} port forward rules", rules.Count);

        try
        {
            // IPアドレスのバリデーション
            foreach (var rule in rules)
            {
                // IPアドレスが無効な場合処理が失敗するため、事前にチェック
                if (!IsValidIpAddress(rule.DestIp))
                {
                    _logger.LogError("Invalid destination IP address: {DestIp}", rule.DestIp);
                    throw new ArgumentException($"Invalid IP address: {rule.DestIp}");
                }
            }

            // ポートフォワード設定を適用
            await _systemd.ApplyPortForwardRulesAsync(rules, ct);

            _logger.LogInformation("All port forward rules applied successfully");

            // 設定ファイルに書き込む（JSON形式にシリアライズ）
            var json = JsonSerializer.Serialize(rules);
            _configsManager.SetConfig(ConfigKey.PortForwardingList, json);
            await _configsManager.SaveConfig();

            _logger.LogInformation("Port forward rules saved to config file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply port forward rules");
            throw;
        }
    }

    /// <summary>
    /// ポートフォワーディング設定を設定ファイルから取得
    /// </summary> <param name="ct">キャンセルトークン</param>
    /// <returns>取得したポートフォワーディングルールのリスト</returns>
    public async Task<List<PortForwardRuleModel>> GetPortForwardRulesAsync(CancellationToken ct)
    {
        try
        {
            var json = _configsManager.GetConfig(ConfigKey.PortForwardingList);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogInformation("No port forward rules found in config");
                return new List<PortForwardRuleModel>();
            }

            var rules = JsonSerializer.Deserialize<List<PortForwardRuleModel>>(json);

            if (rules == null)
            {
                _logger.LogWarning("Failed to deserialize port forward rules from config");
                return new List<PortForwardRuleModel>();
            }

            _logger.LogInformation("Retrieved {Count} port forward rules from config", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve port forward rules");
            throw;
        }
    }

    /// <summary>
    /// 物理ネットワークインターフェースの一覧を取得
    /// </summary>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>物理ネットワークインターフェース名のリスト</returns>
    public async Task<List<string>> GetNetworkInterfacesAsync(CancellationToken ct)
    {
        try
        {
            var list = await _networkManagerPort.ListNicConfigsAsync(ct);
            _logger.LogInformation("Retrieved {Count} network interfaces", list.Count);
            return list.Select(nic => nic.InterfaceName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve network interfaces");
            throw;
        }
    }

    /// <summary>
    /// IPアドレスの妥当性チェック
    /// </summary>
    private bool IsValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out var address)
               && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }
}
