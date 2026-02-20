using Tmds.DBus;
using GATEWAYCore.DbusInterfaces;
using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Domain.Models;

namespace GATEWAYCore.Infrastructure;

/// <summary>
/// systemd サービスを操作する D-Bus クライアント
/// </summary>
public sealed class SystemdClient : ISystemdClient
{
    private readonly ILogger<SystemdClient> _logger;
    private static Connection? _bus;
    private static readonly object _lock = new object();

    public SystemdClient(ILogger<SystemdClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (_bus == null)
        {
            lock (_lock)
            {
                if (_bus == null)
                {
                    _bus = Connection.System;
                    _bus.ConnectAsync().Wait();
                }
            }
        }
    }

    /// <summary>
    /// D-Bus接続を取得
    /// </summary>
    private static Connection EnsureBus()
    {
        if (_bus == null)
        {
            throw new InvalidOperationException("D-Bus connection is not initialized");
        }
        return _bus;
    }

    /// <summary>
    /// サービスを起動
    /// systemd の StartUnit メソッドを呼び出し、指定されたサービスユニットを起動
    /// </summary>
    /// <param name="serviceName">起動するサービス名</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task StartServiceAsync(string serviceName, CancellationToken ct)
    {
        try
        {
            var bus = EnsureBus();
            // systemd1.Manager の D-Bus プロキシを作成
            var systemd = bus.CreateProxy<ISystemd1Manager>(DbusNames.SystemdService, DbusNames.SystemdPath);

            // エスケープ処理を適用
            var escapedServiceName = SystemdEscape(serviceName);

            var jobPath = await systemd.StartUnitAsync(escapedServiceName, "replace");

            _logger.LogInformation("Started service: {ServiceName}, Job: {JobPath}",
                escapedServiceName, jobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service: {ServiceName}", serviceName);
            throw;
        }
    }

    /// <summary>
    /// ポートフォワーディング設定を適用
    /// saferun@.service を起動して nftable 設定を実行
    /// </summary>
    /// <param name="rules">適用するポートフォワーディングルールのリスト</param>
    /// <param name="ct">キャンセルトークン</param>
    public async Task ApplyPortForwardRulesAsync(
        IEnumerable<PortForwardRuleModel> rules,
        CancellationToken ct)
    {
        var ruleList = rules.ToList();

        _logger.LogInformation("Applying {Count} port forward rules (full sync)", ruleList.Count);

        if (ruleList.Count == 0)
        {
            // 空の場合はクリア
            _logger.LogInformation("No rules provided, clearing all port forwards");
            var serviceNames = "saferun@sync-empty.service";
            await StartServiceAsync(serviceNames, ct);
            return;
        }

        // ルールを "interface:protocol:src_port:dst_ip:dst_port" 形式に変換
        var ruleStrings = ruleList.Select(r =>
            $"{r.InterfaceName}:{(r.IsTcp ? "tcp" : "udp")}:{r.SourcePort}:{r.DestIp}:{r.DestPort}"
        );

        // セミコロンで結合
        var rulesParam = string.Join(";", ruleStrings);

        // Base64Url エンコード
        var encodedRules = Base64UrlEncode(rulesParam);

        // プレフィックス "portfwd-" を付けてサービス名を作成
        var serviceName = $"saferun@portfwd-{encodedRules}.service";

        _logger.LogInformation("Starting sync service with {Count} rules", ruleList.Count);
        _logger.LogDebug("Encoded rules length: {Length}", rulesParam.Length);

        await StartServiceAsync(serviceName, ct);

        _logger.LogInformation("Successfully applied {Count} port forward rules", ruleList.Count);
    }

    /// <summary>
    /// Base64Url エンコード
    /// </summary>
    private static string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes);

        // Base64 → Base64Url 変換
        return base64
            .Replace('+', '-')  // + → -
            .Replace('/', '_')  // / → _
            .TrimEnd('=');      // パディング削除
    }

    /// <summary>
    /// systemd-escape 相当の処理
    /// スペースなどの特殊文字をエスケープ
    /// </summary>
    private static string SystemdEscape(string input)
    {
        if (input.EndsWith(".service"))
        {
            var instancePart = input.Substring(0, input.Length - 8); // ".service" を除去
            var atIndex = instancePart.IndexOf('@');

            if (atIndex >= 0)
            {
                var template = instancePart.Substring(0, atIndex + 1);
                var instance = instancePart.Substring(atIndex + 1);

                // インスタンス名をエスケープ
                var escaped = EscapeInstanceName(instance);
                return $"{template}{escaped}.service";
            }
        }
        return input;
    }

    /// <summary>
    /// インスタンス名の特殊文字をエスケープ
    /// </summary>
    private static string EscapeInstanceName(string instance)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in instance)
        {
            if (c == ' ')
                sb.Append(@"\x20");
            else if (c == '-' && instance.Contains(' '))
                sb.Append(@"\x2d");
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
