using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.DbusInterfaces;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Domain.Logic;
using Tmds.DBus;
using GATEWAYCore.Infrastructure.Configs;

namespace GATEWAYCore.Infrastructure;

/// <summary>
/// D-BusでNetworkManagerを扱うクラス
/// </summary>
public sealed class NetworkManagerPort : INetworkManagerPort
{
    private static Connection? _bus;
    private readonly ILogger<NetworkManagerPort> _logger;
    private static readonly object _lock = new object();
    private readonly ConfigsManager _configs;

    public NetworkManagerPort(ILogger<NetworkManagerPort> logger, ConfigsManager configs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = configs ?? throw new ArgumentNullException(nameof(configs));

        // WSL環境の検出
        if (File.Exists("/proc/version"))
        {
            var version = File.ReadAllText("/proc/version");
            if (version.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
                version.Contains("WSL", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Running in WSL environment. /etc/resolv.conf is managed by WSL.");
            }
        }

        // D-Bus接続の初期化
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
    /// 設定ファイルからネットワーク設定を取得
    /// </summary> <returns>構築された NetworkConfigModel</returns>
    public NetworkConfigModel LoadNetworkConfigsFromConfigFile()
    {
        try
        {
            var networks = new List<NicConfigModel>
            {
                new NicConfigModel(
                    InterfaceName: "eth0",
                    MacAddress: "",
                    Ipv4: new Ipv4ConfigModel(
                        Method: _configs.GetConfig(ConfigKey.Eth0Dhcp) == "1" ? Ipv4MethodModel.Dhcp : Ipv4MethodModel.Static,
                        Address: TryGetIpAddress(ConfigKey.Eth0Ip),
                        Prefix: ConvertSubnetMaskToPrefix(_configs.GetConfig(ConfigKey.Eth0Mask) ?? "")
                        ),
                    devicePath: new ObjectPath("/"),
                    connectionPath: new ObjectPath("/"),
                    connectionId: ""),
                new NicConfigModel(
                    InterfaceName: "eth1",
                    MacAddress: "",
                    Ipv4: new Ipv4ConfigModel(
                        Method: _configs.GetConfig(ConfigKey.Eth1Dhcp) == "1" ? Ipv4MethodModel.Dhcp : Ipv4MethodModel.Static,
                        Address: TryGetIpAddress(ConfigKey.Eth1Ip),
                        Prefix: ConvertSubnetMaskToPrefix(_configs.GetConfig(ConfigKey.Eth1Mask) ?? "")
                        ),
                    devicePath: new ObjectPath("/"),
                    connectionPath: new ObjectPath("/"),
                    connectionId: ""),
                new NicConfigModel(
                    InterfaceName: "eth2",
                    MacAddress: "",
                    Ipv4: new Ipv4ConfigModel(
                        Method: _configs.GetConfig(ConfigKey.Eth2Dhcp) == "1" ? Ipv4MethodModel.Dhcp : Ipv4MethodModel.Static,
                        Address: TryGetIpAddress(ConfigKey.Eth2Ip),
                        Prefix: ConvertSubnetMaskToPrefix(_configs.GetConfig(ConfigKey.Eth2Mask) ?? "")
                        ),
                    devicePath: new ObjectPath("/"),
                    connectionPath: new ObjectPath("/"),
                    connectionId: ""),
            };

            var dns = new DnsConfigModel(
                Servers: new List<string>
                {
                    TryGetIpAddress(ConfigKey.EthDnsFirst) ?? "",
                    TryGetIpAddress(ConfigKey.EthDnsSecond) ?? ""
                }
            );

            var otherConfigs = new OtherNetworkConfigs(
                ComEthEndDisable: _configs.GetConfig(ConfigKey.ComEthEndDisable) ?? "",
                ComEthWaitEnable: _configs.GetConfig(ConfigKey.ComEthWaitEnable) ?? "",
                ComEthWaitChange: _configs.GetConfig(ConfigKey.ComEthWaitChange) ?? ""
            );

            var gateway = TryGetIpAddress(ConfigKey.EthGateway) ?? "";

            var networkConfigs = new NetworkConfigModel(networks, gateway, dns);
            return networkConfigs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load network settings from ConfigsManager");
            throw;
        }
    }

    /// <summary>
    /// 保持されているネットワーク設定を取得
    /// </summary>
    //public NetworkConfigModel GetNetworkSettings() => _networkConfigs;

    /// <summary>
    /// サブネットマスクをプレフィックス長に変換
    /// </summary>
    /// <param name="subnetMask">サブネットマスク (例: "255.255.255.0")</param>
    /// <returns>プレフィックス長 (例: 24)</returns>
    private int ConvertSubnetMaskToPrefix(string subnetMask)
    {
        if (string.IsNullOrEmpty(subnetMask))
        {
            return 24;
        }

        try
        {
            var parts = subnetMask.Split('.');
            if (parts.Length != 4)
            {
                return 24;
            }

            int prefix = 0;
            foreach (var part in parts)
            {
                if (!byte.TryParse(part, out var octet))
                {
                    return 24;
                }
                prefix += System.Numerics.BitOperations.PopCount(octet);
            }
            return prefix;
        }
        catch
        {
            return 24;
        }
    }

    /// <summary>
    /// D-Bus接続の null チェック
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Connection EnsureBus()
    {
        if (_bus == null)
        {
            throw new InvalidOperationException("D-Bus connection is not initialized");
        }
        return _bus;
    }

    /// <summary>
    /// NIC設定リストからデバイスパスを解決する
    /// </summary>
    /// <param name="nicConfigs">NIC設定のリスト</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>デバイスパスが解決されたNIC設定のリスト</returns>
    public async Task<List<NicConfigModel>> ResolveDevicePathsAsync(List<NicConfigModel> nicConfigs, CancellationToken ct)
    {
        var bus = EnsureBus();
        var nm = bus.CreateProxy<INetworkManager>(DbusNames.Service, DbusNames.NmRoot);
        var devicePaths = await nm.GetDevicesAsync();

        var deviceMap = new Dictionary<(string interfaceName, string macAddress), ObjectPath>();

        foreach (var devPath in devicePaths)
        {
            ct.ThrowIfCancellationRequested();

            var device = bus.CreateProxy<INetworkDevice>(DbusNames.Service, devPath);
            var interfaceName = await device.GetAsync<string>("Interface");

            var hwAddress = "";
            try
            {
                hwAddress = await device.GetAsync<string>("HwAddress");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get HwAddress for {DevicePath}", devPath);
            }

            if (!string.IsNullOrEmpty(interfaceName) && !string.IsNullOrEmpty(hwAddress))
            {
                deviceMap[(interfaceName, hwAddress)] = devPath;
            }
        }

        var result = new List<NicConfigModel>();
        foreach (var nicConfig in nicConfigs)
        {
            ct.ThrowIfCancellationRequested();

            if (deviceMap.TryGetValue((nicConfig.InterfaceName, nicConfig.MacAddress), out var matchedDevPath))
            {
                result.Add(new NicConfigModel(
                    InterfaceName: nicConfig.InterfaceName,
                    MacAddress: nicConfig.MacAddress,
                    Ipv4: nicConfig.Ipv4,
                    devicePath: matchedDevPath,
                    connectionPath: "/",
                    connectionId: ""
                ));
            }
            else
            {
                _logger.LogWarning("Device not found for interface {InterfaceName} with MAC {MacAddress}",
                    nicConfig.InterfaceName, nicConfig.MacAddress);
                result.Add(nicConfig);
            }
        }

        return result;
    }

    /// <summary>
    /// NIC設定リストからアクティブな接続を解決する
    /// </summary>
    /// <param name="nicConfigs">NIC設定のリスト</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>アクティブな接続が解決されたNIC設定のリスト</returns>
    public async Task<List<NicConfigModel>> ResolveActiveConnection(List<NicConfigModel> nicConfigs, CancellationToken ct)
    {
        var bus = EnsureBus();
        var result = new List<NicConfigModel>();

        foreach (var nicConfig in nicConfigs)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var device = bus.CreateProxy<INetworkDevice>(DbusNames.Service, nicConfig.devicePath);
                var activeConnPath = await device.GetAsync<ObjectPath>("ActiveConnection");
                _logger.LogInformation("activeConnectionPath : {activeConnPath}", activeConnPath.ToString());

                if (activeConnPath.ToString() == "/" || activeConnPath == ObjectPath.Root)
                {
                    _logger.LogDebug("No active connection for {Interface}", nicConfig.InterfaceName);
                    result.Add(nicConfig);
                    continue;
                }

                var activeConn = bus.CreateProxy<IActiveConnection>(DbusNames.Service, activeConnPath);
                var connectionPath = await activeConn.GetAsync<ObjectPath>("Connection");
                _logger.LogInformation("ConnectionPath : {connectionPath}", connectionPath.ToString());

                if (connectionPath == ObjectPath.Root || connectionPath.ToString() == "/")
                {
                    _logger.LogWarning("Invalid connection path for {Interface}", nicConfig.InterfaceName);
                    result.Add(nicConfig);
                    continue;
                }

                string connectionId = nicConfig.InterfaceName;
                try
                {
                    connectionId = await activeConn.GetAsync<string>("Id");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get connection ID for {Interface}", nicConfig.InterfaceName);
                }

                var updatedNicConfig = new NicConfigModel(
                    InterfaceName: nicConfig.InterfaceName,
                    MacAddress: nicConfig.MacAddress,
                    Ipv4: nicConfig.Ipv4,
                    devicePath: nicConfig.devicePath,
                    connectionPath: connectionPath,
                    connectionId: connectionId
                );

                _logger.LogInformation("Resolved connection for {Interface}: {ConnectionId}",
                    nicConfig.InterfaceName, connectionId);

                result.Add(updatedNicConfig);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve connection for {Interface}", nicConfig.InterfaceName);
                result.Add(nicConfig);
            }
        }

        return result;
    }

    /// <summary>
    /// NICのIPv4設定を更新する
    /// </summary>
    /// <param name="nicConfig">NIC設定</param>
    /// <param name="isUplink">アップリンクかどうか</param>
    /// <param name="defaultGateway">デフォルトゲートウェイ</param>
    /// <param name="Dns">DNS設定</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateSettingsAsync(NicConfigModel nicConfig, bool isUplink, string defaultGateway, DnsConfigModel Dns, CancellationToken ct)
    {
        var bus = EnsureBus();

        if (nicConfig.connectionPath.ToString() == "/" || nicConfig.connectionPath == ObjectPath.Root)
        {
            _logger.LogWarning("Invalid connection path for {Interface}", nicConfig.InterfaceName);
            throw new InvalidOperationException($"Invalid connection path for interface {nicConfig.InterfaceName}");
        }

        try
        {
            _logger.LogInformation("Updating settings for {Interface} (Uplink={IsUplink})",
                nicConfig.InterfaceName, isUplink);

            var settingsConnection = bus.CreateProxy<ISettingsConnection>(DbusNames.Service, nicConfig.connectionPath);
            var settings = await settingsConnection.GetSettingsAsync();

            var ipv4Settings = settings.ContainsKey("ipv4") && settings["ipv4"] is Dictionary<string, object> existing
                ? new Dictionary<string, object>(existing)
                : new Dictionary<string, object>();

            // メソッド設定
            ipv4Settings["method"] = nicConfig.Ipv4.Method == Ipv4MethodModel.Static ? "manual" : "auto";

            if (nicConfig.Ipv4.Method == Ipv4MethodModel.Static)
            {
                var addressDict = new Dictionary<string, object>
                {
                    { "address", nicConfig.Ipv4.Address! },
                    { "prefix", (uint)nicConfig.Ipv4.Prefix! },
                };
                ipv4Settings["address-data"] = new IDictionary<string, object>[] { addressDict };

                // 古い形式のフィールドを削除
                ipv4Settings.Remove("addresses");
                ipv4Settings.Remove("routes");
                ipv4Settings.Remove("route-data");

                // ゲートウェイ設定
                if (isUplink && !string.IsNullOrEmpty(defaultGateway))
                {
                    ipv4Settings["gateway"] = defaultGateway;
                }
                else
                {
                    ipv4Settings.Remove("gateway");
                }
            }
            else
            {
                // DHCP の場合: Static関連の設定を削除
                ipv4Settings.Remove("address-data");
                ipv4Settings.Remove("addresses");
                ipv4Settings.Remove("gateway");
                ipv4Settings.Remove("routes");
                ipv4Settings.Remove("route-data");
            }

            // DNS 設定
            if (isUplink && Dns.Servers.Count > 0)
            {
                var dnsUints = Dns.Servers
                    .Where(dns => !string.IsNullOrEmpty(dns))
                    .Select(IpAddressConverter.Ipv4ToUint32LittleEndian)
                    .ToArray();

                if (dnsUints.Length > 0)
                {
                    _logger.LogInformation("Applying DNS: {DnsServers}", string.Join(", ", Dns.Servers));
                    ipv4Settings["dns"] = dnsUints;
                    ipv4Settings.Remove("dns-data");
                }
                else
                {
                    ipv4Settings.Remove("dns");
                    ipv4Settings.Remove("dns-data");
                }
            }
            else
            {
                ipv4Settings.Remove("dns");
                ipv4Settings.Remove("dns-data");
            }

            settings["ipv4"] = ipv4Settings;
            await settingsConnection.UpdateAsync(settings);

            var nm = bus.CreateProxy<INetworkManager>(DbusNames.Service, DbusNames.NmRoot);
            await nm.ActivateConnectionAsync(nicConfig.connectionPath, nicConfig.devicePath, new ObjectPath("/"));

            _logger.LogInformation("Settings updated for {Interface}", nicConfig.InterfaceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for {Interface}", nicConfig.InterfaceName);
            throw;
        }
    }

    /// <summary>
    ///   NetworkManager上のデバイスから、物理かつイーサネットのインタフェースの一覧を取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>NIC設定のリスト</returns>
    public async Task<List<NicConfigModel>> ListNicConfigsAsync(CancellationToken ct)
    {
        var bus = EnsureBus();
        var nm = bus.CreateProxy<INetworkManager>(DbusNames.Service, DbusNames.NmRoot);
        var devicePaths = await nm.GetDevicesAsync();

        var list = new List<NicConfigModel>();

        foreach (var devPath in devicePaths)
        {
            ct.ThrowIfCancellationRequested();

            var device = bus.CreateProxy<INetworkDevice>(DbusNames.Service, devPath);
            var interfaceName = await device.GetAsync<string>("Interface");

            var hwAddress = "";
            try
            {
                hwAddress = await device.GetAsync<string>("HwAddress");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get HwAddress for {DevicePath}", devPath);
            }

            uint deviceType = 0;
            try
            {
                deviceType = await device.GetAsync<uint>("DeviceType");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get DeviceType for {DevicePath}", devPath);
            }

            if (deviceType != 1) // Ethernet のみ
            {
                _logger.LogDebug("Skip non-ethernet device {Name} (Type={Type})", interfaceName, deviceType);
                continue;
            }

            if (string.IsNullOrEmpty(interfaceName) || interfaceName == "lo")
            {
                _logger.LogDebug("Skip interface {Name}", interfaceName ?? "(null)");
                continue;
            }

            var nicConfig = new NicConfigModel(interfaceName, hwAddress,
                new Ipv4ConfigModel(Ipv4MethodModel.Dhcp, null, null),
                devPath, new ObjectPath("/"), "");
            list.Add(nicConfig);
        }

        return list;
    }

    /// <summary>
    /// DNSサーバーとデフォルトゲートウェイを取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>DNSサーバーのリストとデフォルトゲートウェイのアドレス</returns>
    public async Task<(List<string> dnsServers, string? gateway)> GetDnsAndGatewayAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var dnsServers = await ReadDnsServersFromNetworkManagerAsync(ct);
        var gateway = await ReadDefaultGatewayAsync(ct);

        return (dnsServers, gateway);
    }

    /// <summary>
    /// NIC毎にIPv4設定を取得する
    /// </summary>
    /// <param name="nicConfigs">NIC設定のリスト</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>更新されたNIC設定のリスト</returns>
    public async Task<List<NicConfigModel>> GetIpv4Configs(List<NicConfigModel> nicConfigs, CancellationToken ct)
    {
        var bus = EnsureBus();
        var updatedConfigs = new List<NicConfigModel>();

        foreach (var nic in nicConfigs)
        {
            ct.ThrowIfCancellationRequested();

            if (nic.devicePath.ToString() == "/" || nic.devicePath == ObjectPath.Root)
            {
                _logger.LogWarning("Invalid device path for {Interface}", nic.InterfaceName);
                updatedConfigs.Add(nic);
                continue;
            }

            try
            {
                var device = bus.CreateProxy<INetworkDevice>(DbusNames.Service, nic.devicePath);
                var ip4ConfigPath = await device.GetAsync<ObjectPath>("Ip4Config");

                if (ip4ConfigPath.ToString() != "/" && ip4ConfigPath != ObjectPath.Root)
                {
                    var config4 = bus.CreateProxy<IIP4Config>(DbusNames.Service, ip4ConfigPath);
                    var addressData = await config4.GetAsync<IDictionary<string, object>[]>("AddressData");

                    if (addressData.Length > 0)
                    {
                        var firstAddr = addressData[0];
                        if (firstAddr.TryGetValue("address", out var addr) && addr is string address &&
                            firstAddr.TryGetValue("prefix", out var prefixObj) && prefixObj is uint prefix)
                        {
                            var ipv4Method = Ipv4MethodModel.Dhcp;

                            // 1. 接続設定から method を取得
                            if (nic.connectionPath.ToString() != "/" && nic.connectionPath != ObjectPath.Root)
                            {
                                try
                                {
                                    var settings = bus.CreateProxy<ISettingsConnection>(DbusNames.Service, nic.connectionPath);
                                    var connectionSettings = await settings.GetSettingsAsync();

                                    if (connectionSettings.TryGetValue("ipv4", out var ipv4SettingsObj) &&
                                        ipv4SettingsObj is IDictionary<string, object> ipv4Settings)
                                    {
                                        if (ipv4Settings.TryGetValue("method", out var methodObj) &&
                                            methodObj is string method)
                                        {
                                            ipv4Method = method == "manual" ? Ipv4MethodModel.Static : Ipv4MethodModel.Dhcp;
                                            _logger.LogDebug("Method for {Interface}: {Method} (from connection settings)",
                                                nic.InterfaceName, ipv4Method);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to get connection settings for {Interface}", nic.InterfaceName);
                                }
                            }

                            // 2. DHCP4Config の存在で判定（フォールバック）
                            if (ipv4Method == Ipv4MethodModel.Dhcp) // まだ確定していない場合
                            {
                                try
                                {
                                    var dhcp4ConfigPath = await device.GetAsync<ObjectPath>("Dhcp4Config");
                                    if (dhcp4ConfigPath.ToString() == "/" || dhcp4ConfigPath == ObjectPath.Root)
                                    {
                                        // DHCP4Config が無効 → Static の可能性
                                        _logger.LogDebug("No DHCP4Config for {Interface}, assuming Static", nic.InterfaceName);
                                        ipv4Method = Ipv4MethodModel.Static;
                                    }
                                    else
                                    {
                                        _logger.LogDebug("DHCP4Config found for {Interface}, method is DHCP", nic.InterfaceName);
                                        ipv4Method = Ipv4MethodModel.Dhcp;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to get DHCP4Config for {Interface}, assuming DHCP", nic.InterfaceName);
                                    // エラーの場合は DHCP と仮定（デフォルト）
                                }
                            }

                            updatedConfigs.Add(new NicConfigModel(
                                nic.InterfaceName,
                                nic.MacAddress,
                                new Ipv4ConfigModel(ipv4Method, address, (int)prefix),
                                nic.devicePath,
                                nic.connectionPath,
                                nic.connectionId));
                            continue;
                        }
                    }
                }

                // IP4Config が無効な場合
                _logger.LogDebug("No IP4Config for {Interface}", nic.InterfaceName);
                updatedConfigs.Add(nic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get IPv4 config for {Interface}", nic.InterfaceName);
                updatedConfigs.Add(nic);
            }
        }

        return updatedConfigs;
    }

    /// <summary>
    /// NetworkManager の IP4Config から DNS サーバーを取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>DNSサーバーのリスト</returns>
    private async Task<List<string>> ReadDnsServersFromNetworkManagerAsync(CancellationToken ct)
    {
        var bus = EnsureBus();
        var dnsList = new List<string>();

        try
        {
            var nm = bus.CreateProxy<INetworkManager>(DbusNames.Service, DbusNames.NmRoot);
            var devicePaths = await nm.GetDevicesAsync();

            foreach (var devPath in devicePaths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var device = bus.CreateProxy<INetworkDevice>(DbusNames.Service, devPath);
                    var ip4ConfigPath = await device.GetAsync<ObjectPath>("Ip4Config");

                    if (ip4ConfigPath.ToString() == "/" || ip4ConfigPath == ObjectPath.Root)
                        continue;

                    var config4 = bus.CreateProxy<IIP4Config>(DbusNames.Service, ip4ConfigPath);
                    var dnsData = await config4.GetAsync<uint[]>("Nameservers");

                    if (dnsData != null && dnsData.Length > 0)
                    {
                        foreach (var dnsUint in dnsData)
                        {
                            var dnsStr = IpAddressConverter.Uint32ToIpv4LittleEndian(dnsUint);
                            if (!dnsList.Contains(dnsStr))
                            {
                                dnsList.Add(dnsStr);
                            }
                        }

                        if (dnsList.Count > 0)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get DNS from device {DevicePath}", devPath);
                }
            }

            if (dnsList.Count > 0)
            {
                _logger.LogInformation("DNS servers: {DnsServers}", string.Join(", ", dnsList));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read DNS servers from NetworkManager");
        }

        return dnsList;
    }

    /// <summary>
    /// デフォルトゲートウェイを /proc/net/route から読み取る
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>デフォルトゲートウェイのアドレス</returns>
    private async Task<string?> ReadDefaultGatewayAsync(CancellationToken ct)
    {
        const string routePath = "/proc/net/route";

        if (!File.Exists(routePath))
        {
            _logger.LogWarning("Route file not found at {Path}", routePath);
            return null;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(routePath, ct);
            string? bestGateway = null;
            int bestMetric = int.MaxValue;

            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();

                var fields = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length > 0 && fields[0] == "Iface")
                    continue;

                if (fields.Length >= 7 && string.Equals(fields[1], "00000000", StringComparison.OrdinalIgnoreCase))
                {
                    if (uint.TryParse(fields[2], System.Globalization.NumberStyles.HexNumber, null, out var gatewayHex) &&
                        int.TryParse(fields[6], out var metric))
                    {
                        if (metric < bestMetric)
                        {
                            bestMetric = metric;
                            var bytes = BitConverter.GetBytes(gatewayHex);
                            bestGateway = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
                        }
                    }
                }
            }

            return bestGateway;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read default gateway from {Path}", routePath);
        }

        return null;
    }

    /// <summary>
    /// IP アドレス設定を uint で取得して IP 文字列に変換
    /// </summary>
    private string TryGetIpAddress(ConfigKey key)
    {
        var uintValue = _configs.GetConfig<uint>(key);
        if (uintValue == 0)
        {
            return "";
        }
        return IpAddressConverter.Uint32ToIpv4BigEndian(uintValue);
    }
}
