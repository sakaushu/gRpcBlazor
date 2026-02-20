using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Infrastructure.Configs;

namespace GATEWAYCore.Application.UseCase;

public sealed class TimeConfigUseCase
{
    private readonly ISystemdTimedateClient _client;
    private readonly ILogger<TimeConfigUseCase> _logger;

    private readonly ConfigsManager _configsManager;

    const string CONFIG_PATH = "/app/mnt/settings/debian/timesyncd.conf";

    public TimeConfigUseCase(ISystemdTimedateClient client, ILogger<TimeConfigUseCase> logger)
    {
        _client = client;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// システム時刻を取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>システム時刻</returns>
    public async Task<DateTime> GetSystemTimeAsync(CancellationToken ct)
    {
        var usecUtc = await _client.GetSystemTimeAsync(ct);
        return DateTimeOffset.FromUnixTimeMilliseconds(usecUtc / 1000).UtcDateTime;
    }

    /// <summary>
    /// システム時刻を設定
    /// </summary>
    /// <param name="dateTime">設定する日時（UTC）</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task ApplySystemTimeAsync(DateTime dateTime, CancellationToken ct)
    {
        await _client.ApplySystemTimeAsync(dateTime, ct);
    }


    /// <summary>
    /// 利用可能なタイムゾーン一覧を取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>利用可能なタイムゾーンIDのリスト</returns>
    public async Task<List<string>> ListTimezonesAsync(CancellationToken ct)
    {
        return await _client.ListTimezonesAsync(ct);
    }

    /// <summary>
    /// 現在のタイムゾーンIDを取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>現在のタイムゾーンID</returns>
    public async Task<string> GetTimezoneAsync(CancellationToken ct)
    {
        return await _client.GetTimezoneAsync(ct);
    }

    /// <summary>
    /// タイムゾーンを設定
    /// </summary>
    /// <param name="timezone">タイムゾーンID</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task SetTimezoneAsync(string timezone, CancellationToken ct)
    {
        await _client.SetTimezoneAsync(timezone, ct);
    }

    /// <summary>
    /// NTPの有効/無効状態を取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>NTPの有効/無効状態</returns>
    public async Task<bool> GetNTPConfigAsync(CancellationToken ct)
    {
        return await _client.GetNTPConfigAsync(ct);
    }

    /// <summary>
    /// NTPの有効/無効を設定
    /// </summary>
    /// <param name="enable">NTPの有効/無効状態</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task SetNTPAsync(bool enable, CancellationToken ct)
    {
        await _client.SetNTPAsync(enable, ct);
    }

    /// <summary>
    /// 現在のNTPサーバーを取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>現在のNTPサーバー</returns>
    public async Task<string> GetNTPServerAsync(CancellationToken ct)
    {
        try
        {
            return await _client.GetNTPServerAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to get NTP server from config file.");
            throw;
        }
    }

    /// <summary>
    /// NTPサーバーを設定
    /// </summary>
    /// <param name="ntpServer">設定するNTPサーバー</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task SetNTPServerAsync(string ntpServer, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            const string configPath = CONFIG_PATH;
            const string backupPath = $"{CONFIG_PATH}.bak";

            // 設定ファイルのバックアップを作成
            if (File.Exists(configPath))
            {
                File.Copy(configPath, backupPath, true);
                _logger.LogInformation($"設定ファイルをバックアップしました: {backupPath}");
            }

            // 設定ファイルを読み込み
            string[] lines;
            if (File.Exists(configPath))
            {
                lines = await File.ReadAllLinesAsync(configPath);
            }
            else
            {
                lines = new[] { "[Time]" };
            }

            // NTP= の行を探して書き換え
            bool ntpLineFound = false;
            bool inTimeSection = false;
            var newLines = new List<string>();

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                // [Time] セクションを検出
                if (trimmedLine == "[Time]")
                {
                    inTimeSection = true;
                    newLines.Add(line);
                    continue;
                }

                // 別のセクションに入ったら [Time] セクション終了
                if (trimmedLine.StartsWith("[") && trimmedLine != "[Time]")
                {
                    // [Time] セクション内で NTP= が見つからなかった場合、追加
                    if (inTimeSection && !ntpLineFound)
                    {
                        newLines.Add($"NTP={ntpServer}");
                        ntpLineFound = true;
                    }
                    inTimeSection = false;
                }

                // [Time] セクション内の NTP= 行を書き換え
                if (inTimeSection && (trimmedLine.StartsWith("NTP=") || trimmedLine.StartsWith("#NTP=")))
                {
                    newLines.Add($"NTP={ntpServer}");
                    ntpLineFound = true;
                    continue;
                }

                newLines.Add(line);
            }

            // [Time] セクションが最後まで続いていて NTP= がなかった場合
            if (inTimeSection && !ntpLineFound)
            {
                newLines.Add($"NTP={ntpServer}");
                ntpLineFound = true;
            }

            // [Time] セクション自体がなかった場合
            if (!ntpLineFound)
            {
                newLines.Add("");
                newLines.Add("[Time]");
                newLines.Add($"NTP={ntpServer}");
            }

            // 設定ファイルに書き込み
            await File.WriteAllLinesAsync(configPath, newLines);

            // systemd-timesyncd を再起動
            await _client.RestartTimesyncServiceAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to set NTP server in config file.");
            throw;
        }
    }
}
