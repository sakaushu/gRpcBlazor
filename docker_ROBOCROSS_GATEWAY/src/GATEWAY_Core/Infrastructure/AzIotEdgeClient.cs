using GATEWAYCore;
using GATEWAYCore.Application.Abstractions;

namespace GATEWAYCore.Infrastructure;

/// <summary>
/// Azure IoT Edge設定クライアント
/// </summary>
public sealed class AzIotEdgeClient : IAzIotEdgeClient
{
    private readonly ILogger<AzIotEdgeClient> _logger;

    // config.tomlのパス
    private const string ConfigPath = "/etc/aziot/config.toml";

    public AzIotEdgeClient(ILogger<AzIotEdgeClient> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Azure IoT Edgeのステータスを取得
    /// </summary>
    /// <returns>IoT Edgeのステータス情報</returns>
    public async Task<Dictionary<string, string>> GetStatusAsync()
    {
        try
        {
            string systemStatus = await ExecuteCommandAsync("iotedge", "system status");
            return ParseSystemStatus(systemStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Azure IoT Edge status");
            throw;
        }
    }

    /// <summary>
    /// Azure IoT Edgeモジュール情報を取得
    /// </summary>
    /// <returns>モジュール情報のリスト</returns>
    public async Task<List<AzIotEdgeModuleInfo>> GetModuleInfoAsync()
    {
        try
        {
            string moduleListOutput = await ExecuteCommandAsync("iotedge", "list");
            return ParseModuleList(moduleListOutput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Azure IoT Edge module info");
            throw;
        }
    }

    /// <summary>
    /// Azure IoT Edge接続設定を取得
    /// </summary>
    /// <returns>接続設定情報</returns>
    public async Task<AzIotEdgeConnectionConfig> GetConnectionConfigAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                _logger.LogWarning("IoT Edge configuration file not found: {ConfigPath}", ConfigPath);
                return CreateDefaultConfig("not found");
            }

            return await ParseTomlConfigAsync(ConfigPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access to the configuration file is denied");
            return CreateDefaultConfig("access denied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting IoT Edge connection config");
            return CreateDefaultConfig("error");
        }
    }

    /// <summary>
    /// Azure IoT Edge接続設定を更新
    /// </summary>
    /// <param name="config">新しい接続設定</param>
    /// <returns></returns>
    public async Task UpdateConnectionConfigAsync(AzIotEdgeConnectionConfig config)
    {
        try
        {
            _logger.LogInformation("Starting connection config update. IsX509={IsX509}", config.IsX509);

            if (config.IsX509)
            {
                _logger.LogInformation("Updating config.toml file directly for X.509");
                await UpdateAzIotEdgeConnectionConfigFileAsync(config);
            }
            else
            {
                _logger.LogInformation("Using connection string method");
                await ExecuteCommandAsync("iotedge", $"config mp --force --connection-string \"{config.ConnectionString}\"");
            }

            _logger.LogInformation("Applying configuration changes...");
            var applyOutput = await ExecuteCommandAsync("aziotctl", "config apply");
            _logger.LogInformation("aziotctl config apply output: {Output}", applyOutput);

            _logger.LogInformation("IoT Edge settings update completed");

            // 更新後の設定を確認
            var updatedConfig = await GetConnectionConfigAsync();
            _logger.LogInformation("Updated config verification: HostName={HostName}, DeviceId={DeviceId}",
                updatedConfig.HostName, updatedConfig.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IoT Edge connection config");
            throw;
        }
    }

    /// <summary>
    /// コマンドを非同期で実行
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<string> ExecuteCommandAsync(string fileName, params string[] args)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"{fileName} command failed (exit code: {process.ExitCode}): {error}");
            }

            return output;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            throw new Exception($"{fileName} command not found. Please ensure it is installed.");
        }
    }

    /// <summary>
    /// デフォルト設定を作成
    /// </summary>
    /// <param name="defaultValue">デフォルト値</param>
    /// <returns>デフォルトの接続設定</returns>
    private AzIotEdgeConnectionConfig CreateDefaultConfig(string defaultValue)
    {
        return new AzIotEdgeConnectionConfig
        {
            ConnectionString = defaultValue,
            HostName = defaultValue,
            DeviceId = defaultValue,
            IsX509 = false
        };
    }

    /// <summary>
    /// iotedge system statusコマンドの出力をパース
    /// </summary>
    /// <param name="statusOutput">iotedge system statusコマンドの出力</param>
    /// <returns>表示用のステータスマップ</returns>
    private Dictionary<string, string> ParseSystemStatus(string statusOutput)
    {
        var statusMap = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(statusOutput))
            return statusMap;

        var lines = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool inSystemServicesSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("System services:"))
            {
                inSystemServicesSection = true;
                continue;
            }

            if (trimmedLine.StartsWith("Use 'iotedge") || string.IsNullOrWhiteSpace(trimmedLine))
                break;

            if (inSystemServicesSection)
            {
                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    statusMap[parts[0]] = parts[1];
                }
            }
        }
        return statusMap;
    }

    /// <summary>
    /// iotedge listコマンドの出力をパース
    /// </summary>
    /// <param name="listOutput">iotedge listコマンドの出力</param>
    /// <returns>モジュール情報のリスト</returns>
    private List<AzIotEdgeModuleInfo> ParseModuleList(string listOutput)
    {
        var moduleList = new List<AzIotEdgeModuleInfo>();

        if (string.IsNullOrWhiteSpace(listOutput))
            return moduleList;

        var lines = listOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool headerFound = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // ヘッダー行をスキップ
            if (trimmedLine.StartsWith("NAME") && trimmedLine.Contains("STATUS"))
            {
                headerFound = true;
                continue;
            }

            if (!headerFound || string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // 2つ以上の空白で区切られた列をパース
            var parts = System.Text.RegularExpressions.Regex.Split(trimmedLine, @"\s{2,}");

            if (parts.Length >= 2)
            {
                moduleList.Add(new AzIotEdgeModuleInfo
                {
                    Name = parts[0].Trim(),
                    Status = parts[1].Trim(),
                    Description = parts.Length > 2 ? parts[2].Trim() : "",
                    Config = parts.Length > 3 ? parts[3].Trim() : ""
                });
            }
        }

        return moduleList;
    }

    /// <summary>
    /// TOMLファイルから接続設定をパース
    /// </summary>
    /// <param name="configPath">TOMLファイルのパス</param>
    /// <returns>接続設定</returns>
    private async Task<AzIotEdgeConnectionConfig> ParseTomlConfigAsync(string configPath)
    {
        var settings = CreateDefaultConfig("not found");
        var identity = new Dictionary<string, string>();

        var lines = await File.ReadAllLinesAsync(configPath);
        bool inProvisioningSection = false;
        bool inAttestationSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("#") || string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // セクションの切り替え
            if (trimmedLine.StartsWith("[provisioning.authentication]"))
            {
                inProvisioningSection = false;
                inAttestationSection = true;
                continue;
            }

            if (trimmedLine.StartsWith("[provisioning]"))
            {
                inProvisioningSection = true;
                inAttestationSection = false;
                continue;
            }

            if (trimmedLine.StartsWith("[") && !trimmedLine.StartsWith("[provisioning"))
            {
                inProvisioningSection = false;
                inAttestationSection = false;
            }

            // プロビジョニング設定の読み取り
            if (inProvisioningSection)
            {
                if (trimmedLine.StartsWith("connection_string"))
                    settings.ConnectionString = ExtractValue(trimmedLine);
                else if (trimmedLine.StartsWith("iothub_hostname"))
                    settings.HostName = ExtractValue(trimmedLine);
                else if (trimmedLine.StartsWith("device_id"))
                    settings.DeviceId = ExtractValue(trimmedLine);
            }

            // 認証設定の読み取り
            if (inAttestationSection)
            {
                if (trimmedLine.StartsWith("method"))
                    settings.IsX509 = ExtractValue(trimmedLine).Equals("x509", StringComparison.OrdinalIgnoreCase);
                else if (trimmedLine.StartsWith("identity_cert"))
                    identity["cert"] = ExtractValue(trimmedLine);
                else if (trimmedLine.StartsWith("identity_pk"))
                    identity["private_key"] = ExtractValue(trimmedLine);
            }
        }

        settings.Identity = new X509Identity
        {
            CertPath = identity.GetValueOrDefault("cert", string.Empty),
            KeyPath = identity.GetValueOrDefault("private_key", string.Empty)
        };

        return settings;
    }

    /// <summary>
    /// TOML行から値を抽出
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private string ExtractValue(string line)
    {
        var parts = line.Split('=', 2);
        return parts.Length == 2 ? parts[1].Trim().Trim('"', '\'') : "not found";
    }

    /// <summary>
    /// AzIotEdgeの接続設定TOMLファイルを更新（X.509専用）
    /// </summary>
    /// <param name="config">接続設定</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private async Task UpdateAzIotEdgeConnectionConfigFileAsync(AzIotEdgeConnectionConfig config)
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException($"Configuration file not found: {ConfigPath}");

        _logger.LogInformation("Updating config for X.509: HostName={HostName}, DeviceId={DeviceId}, CertPath={CertPath}, KeyPath={KeyPath}",
            config.HostName, config.DeviceId, config.Identity?.CertPath, config.Identity?.KeyPath);

        var lines = await File.ReadAllLinesAsync(ConfigPath);
        var updatedLines = new List<string>();

        bool inProvisioningSection = false;
        bool inAuthenticationSection = false;
        bool foundAuthenticationSection = false;
        int provisioningSectionEndIndex = -1;

        // 各項目が更新されたかを追跡
        var updates = new HashSet<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // コメント行と空行はそのまま追加
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                updatedLines.Add(line);
                continue;
            }

            // セクション検出
            if (trimmedLine.StartsWith("["))
            {
                // セクション終了時の不足項目追加
                if (inProvisioningSection)
                {
                    AddMissingProvisioningItems(updatedLines, config, updates);
                    provisioningSectionEndIndex = updatedLines.Count;
                }
                else if (inAuthenticationSection)
                {
                    AddMissingAuthenticationItems(updatedLines, config, updates);
                }

                if (trimmedLine == "[provisioning]")
                {
                    inProvisioningSection = true;
                    inAuthenticationSection = false;
                }
                else if (trimmedLine == "[provisioning.authentication]")
                {
                    foundAuthenticationSection = true;
                    inProvisioningSection = false;
                    inAuthenticationSection = true;
                }
                else if (!trimmedLine.StartsWith("[provisioning"))
                {
                    inProvisioningSection = false;
                    inAuthenticationSection = false;
                }

                updatedLines.Add(line);
                continue;
            }

            // [provisioning] セクション内の更新
            if (inProvisioningSection)
            {
                if (trimmedLine.StartsWith("source"))
                {
                    updatedLines.Add("source = \"manual\"");
                    updates.Add("source");
                }
                else if (trimmedLine.StartsWith("iothub_hostname"))
                {
                    updatedLines.Add($"iothub_hostname = \"{config.HostName}\"");
                    updates.Add("iothub_hostname");
                }
                else if (trimmedLine.StartsWith("device_id"))
                {
                    updatedLines.Add($"device_id = \"{config.DeviceId}\"");
                    updates.Add("device_id");
                }
                else if (trimmedLine.StartsWith("connection_string"))
                {
                    _logger.LogInformation("Removed connection_string");
                    continue; // スキップ
                }
                else
                {
                    updatedLines.Add(line);
                }
            }
            // [provisioning.authentication] セクション内の更新
            else if (inAuthenticationSection)
            {
                if (trimmedLine.StartsWith("method"))
                {
                    updatedLines.Add("method = \"x509\"");
                    updates.Add("method");
                }
                else if (trimmedLine.StartsWith("identity_cert"))
                {
                    updatedLines.Add($"identity_cert = \"{config.Identity?.CertPath}\"");
                    updates.Add("identity_cert");
                }
                else if (trimmedLine.StartsWith("identity_pk"))
                {
                    updatedLines.Add($"identity_pk = \"{config.Identity?.KeyPath}\"");
                    updates.Add("identity_pk");
                }
                else
                {
                    updatedLines.Add(line);
                }
            }
            else
            {
                updatedLines.Add(line);
            }
        }

        // ファイル終端での処理
        if (inProvisioningSection)
        {
            AddMissingProvisioningItems(updatedLines, config, updates);
            provisioningSectionEndIndex = updatedLines.Count;
        }
        else if (inAuthenticationSection)
        {
            AddMissingAuthenticationItems(updatedLines, config, updates);
        }

        // [provisioning.authentication]セクションが存在しない場合は作成
        if (!foundAuthenticationSection && provisioningSectionEndIndex > 0)
        {
            _logger.LogInformation("Creating [provisioning.authentication] section");
            var authLines = new List<string>
            {
                "",
                "[provisioning.authentication]",
                "method = \"x509\"",
                $"identity_cert = \"{config.Identity?.CertPath}\"",
                $"identity_pk = \"{config.Identity?.KeyPath}\""
            };
            updatedLines.InsertRange(provisioningSectionEndIndex, authLines);
        }

        _logger.LogInformation("Config update completed. Updated items: {Updates}", string.Join(", ", updates));

        await File.WriteAllLinesAsync(ConfigPath, updatedLines);
    }

    /// <summary>
    /// [provisioning]セクションの不足項目を追加
    /// </summary>
    private void AddMissingProvisioningItems(List<string> lines, AzIotEdgeConnectionConfig config, HashSet<string> updates)
    {
        if (!updates.Contains("iothub_hostname"))
        {
            lines.Add($"iothub_hostname = \"{config.HostName}\"");
            _logger.LogInformation("Added iothub_hostname = {HostName}", config.HostName);
        }
        if (!updates.Contains("device_id"))
        {
            lines.Add($"device_id = \"{config.DeviceId}\"");
            _logger.LogInformation("Added device_id = {DeviceId}", config.DeviceId);
        }
    }

    /// <summary>
    /// [provisioning.authentication]セクションの不足項目を追加
    /// </summary>
    private void AddMissingAuthenticationItems(List<string> lines, AzIotEdgeConnectionConfig config, HashSet<string> updates)
    {
        if (config.Identity == null) return;

        if (!updates.Contains("identity_cert"))
        {
            lines.Add($"identity_cert = \"{config.Identity.CertPath}\"");
            _logger.LogInformation("Added identity_cert = {CertPath}", config.Identity.CertPath);
        }
        if (!updates.Contains("identity_pk"))
        {
            lines.Add($"identity_pk = \"{config.Identity.KeyPath}\"");
            _logger.LogInformation("Added identity_pk = {KeyPath}", config.Identity.KeyPath);
        }
    }
}
