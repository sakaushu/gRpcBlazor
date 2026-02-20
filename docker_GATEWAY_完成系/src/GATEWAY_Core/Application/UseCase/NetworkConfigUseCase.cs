using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Domain.Logic;
using GATEWAYCore.Infrastructure.Configs;

namespace GATEWAYCore.Application.UseCase;

/// <summary>
/// ネットワーク設定ユースケース
/// </summary>
public sealed class NetworkConfigUseCase
{
    private readonly INetworkManagerPort _nm;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<NetworkConfigUseCase> _logger;

    private readonly ConfigsManager _configsManager;

    public NetworkConfigUseCase(INetworkManagerPort nm, ILogger<NetworkConfigUseCase> logger, ConfigsManager configsManager)
    {
        _nm = nm;
        _logger = logger;
        _configsManager = configsManager;
    }

    /// <summary>
    /// ネットワーク設定を取得する(OS設定値)
    /// </summary>
    /// <param name="ct">キャンセル通知用</param>
    /// <returns>現在のネットワーク設定</returns>
    public async Task<NetworkConfigModel> GetNetworkConfigAsync(CancellationToken ct)
    {
        // NIC毎にインタフェース名と物理アドレスを取得
        var list = await _nm.ListNicConfigsAsync(ct);

        // アクティブな接続情報を解決
        var resolvedList = await _nm.ResolveActiveConnection(list, ct);

        // Ipv4設定を取得
        var ipv4UpdateList = await _nm.GetIpv4Configs(resolvedList, ct);

        //OS共通の設定を取得（DNS・ゲートウェイ）
        var (servers, gateway) = await _nm.GetDnsAndGatewayAsync(ct);
        var dns = new DnsConfigModel(servers!);

        return new NetworkConfigModel(ipv4UpdateList, gateway!, dns);
    }

    /// <summary>
    /// 設定ファイルからネットワーク設定を取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用</param>
    /// <returns>現在のネットワーク設定ファイル内容</returns>
    public async Task<NetworkConfigModel> GetNetworkConfigFromFileAsync(CancellationToken ct)
    {
        // 1. 設定ファイルに保存されているネットワーク設定を取得
        var networkConfig = _nm.LoadNetworkConfigsFromConfigFile();

        // 2. 実際に存在するインタフェース一覧を取得
        var list = await _nm.ListNicConfigsAsync(ct);

        // 3. 実際に存在するインタフェースの数だけNicConfigを作成
        var resultNicConfigs = new List<NicConfigModel>();

        foreach (var detectedNic in list)
        {
            // 設定ファイルから該当するインターフェース名の設定を検索
            var fileConfig = networkConfig.NicConfigs
                .FirstOrDefault(nic => nic.InterfaceName == detectedNic.InterfaceName);

            NicConfigModel nicConfig;

            if (fileConfig != null)
            {
                // 設定ファイルに該当するインターフェース名が存在する場合
                // 設定ファイルの値を使用し、MACアドレスは実際の値を使用
                nicConfig = new NicConfigModel(
                    InterfaceName: detectedNic.InterfaceName ?? string.Empty,
                    MacAddress: detectedNic.MacAddress ?? string.Empty,
                    Ipv4: fileConfig.Ipv4,
                    devicePath: detectedNic.devicePath,
                    connectionPath: detectedNic.connectionPath,
                    connectionId: detectedNic.connectionId ?? string.Empty
                );
            }
            else
            {
                // 設定ファイルに該当するインターフェース名が存在しない場合
                // 空の値で作成
                nicConfig = new NicConfigModel(
                    InterfaceName: detectedNic.InterfaceName ?? string.Empty,
                    MacAddress: detectedNic.MacAddress ?? string.Empty,
                    Ipv4: new Ipv4ConfigModel(Ipv4MethodModel.Dhcp, string.Empty, 24),
                    devicePath: detectedNic.devicePath,
                    connectionPath: detectedNic.connectionPath,
                    connectionId: detectedNic.connectionId ?? string.Empty
                );
            }

            resultNicConfigs.Add(nicConfig);
        }

        // NetworkConfigModelを再構築
        return new NetworkConfigModel(
            NicConfigs: resultNicConfigs,
            DefaultGateway: networkConfig.DefaultGateway ?? "192.168.1.1",
            Dns: networkConfig.Dns ?? new DnsConfigModel(new List<string> { "8.8.8.8" })
        );
    }

    /// <summary>
    /// ネットワーク設定を適用する
    /// </summary>
    /// <param name="request">適用するネットワーク設定</param>
    /// <param name="ct">キャンセル通知用</param>
    /// <returns>適用後のネットワーク設定</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<NetworkConfigModel> ApplyNetworkConfigAsync(NetworkConfigModel request, CancellationToken ct)
    {
        if (request.NicConfigs is null || request.NicConfigs.Count == 0)
        {
            throw new ArgumentException("No NIC Configs.");
        }

        await _lock.WaitAsync(ct);

        try
        {
            // デバイスパスとコネクションパスを特定
            var devicepathResolved = await _nm.ResolveDevicePathsAsync(request.NicConfigs, ct);
            var connectionResolved = await _nm.ResolveActiveConnection(devicepathResolved, ct);

            // アクティブな接続のみフィルタリング
            var activeConnections = connectionResolved
                .Where(nic => nic.connectionPath.ToString() != "/")
                .ToList();

            if (activeConnections.Count == 0)
            {
                throw new InvalidOperationException("No active connections found to update");
            }

            // 各NICの設定を更新
            bool isUplink = true;
            foreach (var nic in activeConnections)
            {
                await _nm.UpdateSettingsAsync(nic, isUplink, request.DefaultGateway, request.Dns, ct);
                isUplink = false;
            }

            // NetworkManager の設定反映を待つ
            await Task.Delay(2000, ct); // 2秒待機

            //設定ファイルにネットワーク設定（NIC毎の設定）を保存する
            for (int i = 0; i < request.NicConfigs.Count && i < 3; i++)
            {
                var nic = request.NicConfigs[i];

                // DHCP設定: enum → int (0 or 1)
                int dhcpValue = nic.Ipv4.Method == Ipv4MethodModel.Dhcp ? 1 : 0;

                // IP設定: string → uint (big-endian)
                uint ipValue = string.IsNullOrEmpty(nic.Ipv4.Address)
                    ? 0
                    : IpAddressConverter.Ipv4ToUint32BigEndian(nic.Ipv4.Address);

                // サブネットマスク設定: int (prefix) → uint (mask)
                uint maskValue = IpAddressConverter.PrefixToUint32Mask(nic.Ipv4.Prefix ?? 24);

                switch (i)
                {
                    case 0: // eth0
                        _configsManager.SetConfig(ConfigKey.Eth0Dhcp, dhcpValue);
                        _configsManager.SetConfig(ConfigKey.Eth0Ip, ipValue);
                        _configsManager.SetConfig(ConfigKey.Eth0Mask, maskValue);
                        break;
                    case 1: // eth1
                        _configsManager.SetConfig(ConfigKey.Eth1Dhcp, dhcpValue);
                        _configsManager.SetConfig(ConfigKey.Eth1Ip, ipValue);
                        _configsManager.SetConfig(ConfigKey.Eth1Mask, maskValue);
                        break;
                    case 2: // eth2
                        _configsManager.SetConfig(ConfigKey.Eth2Dhcp, dhcpValue);
                        _configsManager.SetConfig(ConfigKey.Eth2Ip, ipValue);
                        _configsManager.SetConfig(ConfigKey.Eth2Mask, maskValue);
                        break;
                }
            }

            //設定ファイルにネットワーク設定（ゲートウェイ・DNSサーバー）を保存する
            uint gatewayValue = string.IsNullOrEmpty(request.DefaultGateway)
                ? 0
                : IpAddressConverter.Ipv4ToUint32BigEndian(request.DefaultGateway);
            _configsManager.SetConfig(ConfigKey.EthGateway, gatewayValue);

            if (request.Dns.Servers != null && request.Dns.Servers.Count > 0)
            {
                uint dnsFirst = string.IsNullOrEmpty(request.Dns.Servers[0])
                    ? 0
                    : IpAddressConverter.Ipv4ToUint32BigEndian(request.Dns.Servers[0]);
                _configsManager.SetConfig(ConfigKey.EthDnsFirst, dnsFirst);

                if (request.Dns.Servers.Count > 1)
                {
                    uint dnsSecond = string.IsNullOrEmpty(request.Dns.Servers[1])
                        ? 0
                        : IpAddressConverter.Ipv4ToUint32BigEndian(request.Dns.Servers[1]);
                    _configsManager.SetConfig(ConfigKey.EthDnsSecond, dnsSecond);
                }
                else
                {
                    _configsManager.SetConfig(ConfigKey.EthDnsSecond, 0u);
                }
            }
            else
            {
                _configsManager.SetConfig(ConfigKey.EthDnsFirst, 0u);
                _configsManager.SetConfig(ConfigKey.EthDnsSecond, 0u);
            }
            await _configsManager.SaveConfig();

            // Ipv4設定を取得
            var ipv4UpdateList = await _nm.GetIpv4Configs(connectionResolved, ct);

            // OS共通の設定を取得（DNS・ゲートウェイ）
            var (servers, gateway) = await _nm.GetDnsAndGatewayAsync(ct);

            // null チェック
            var dns = new DnsConfigModel(servers ?? new List<string>());
            var finalGateway = gateway ?? string.Empty;

            return new NetworkConfigModel(ipv4UpdateList, finalGateway, dns);
        }
        finally
        {
            _lock.Release();
        }
    }
}
