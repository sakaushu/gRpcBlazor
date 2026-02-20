using Tmds.DBus;
using GATEWAYCore.DbusInterfaces;
using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Infrastructure.Configs;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Domain.Logic;


namespace GATEWAYCore.Infrastructure;

public sealed class SystemdTimedateClient : ISystemdTimedateClient
{
    private readonly ILogger<SystemdTimedateClient> _logger;
    private readonly ConfigsManager _configs;

    public SystemdTimedateClient(ILogger<SystemdTimedateClient> logger, ConfigsManager configs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = configs ?? throw new ArgumentNullException(nameof(configs));
    }

    /// <summary>
    /// システム時刻を取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>システム時刻</returns>
    public async Task<long> GetSystemTimeAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();
            var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");

            // ulong (uint64) として読み取る
            ulong timeUsec = await td.GetAsync<ulong>("TimeUSec");

            // long に変換
            return (long)timeUsec;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to get system time via D-Bus.");
            throw;
        }
    }

    /// <summary>
    /// システム時刻を設定する
    /// </summary>
    /// <param name="dateTime">設定する日時（UTC）</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task ApplySystemTimeAsync(DateTime dateTime, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            var dto = new DateTimeOffset(dateTime, TimeSpan.Zero); // UTCとして扱う
            bool interactive = false;

            long usecUtc = dto.ToUnixTimeMilliseconds() * 1000;
            bool relative = false; // 絶対時刻でセット

            var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");

            await td.SetTimeAsync(usecUtc, relative, interactive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to apply system time via D-Bus.");
            throw;
        }
    }

    /// <summary>
    /// 利用可能なタイムゾーン一覧を取得
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>利用可能なタイムゾーンIDのリスト</returns>
    public async Task<List<string>> ListTimezonesAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            
            // TimeZoneOffsetHelperから取得（既にUTCオフセット順にソートされている）
            var timezones = TimeZoneOffsetHelper.GetAllTimeZoneIds();

            return await Task.FromResult(timezones.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to list timezones.");
            throw;
        }
    }


    /// <summary>
    /// 現在のタイムゾーンIDを取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>現在のタイムゾーンID</returns>
    public async Task<string> GetTimezoneAsync(CancellationToken ct)
    {
        try
        {
            //// タイムゾーンを取得
            //var timeZone = _configs.GetConfig(ConfigKey.TimeZone);

            ct.ThrowIfCancellationRequested();
            //using var connection = new Connection(Address.System);
            //await connection.ConnectAsync();

            //var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");
            //var timeZoneCurrent = await td.GetAsync<string>("Timezone");
            //if (timeZoneCurrent != timeZone)
            //{
            //    _logger.LogInformation("TimeZone changed from {Setting TimeZone} to {Real TimeZone}", timeZone, timeZoneCurrent);
            //}
            //var actualTimeZone = timeZone ?? timeZoneCurrent;

            //return actualTimeZone;

            return _configs.GetConfig(ConfigKey.TimeZone) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to get timezone via D-Bus.");
            throw;
        }
    }

    /// <summary>
    /// タイムゾーンを設定する
    /// </summary>
    /// <param name="timezone">設定するタイムゾーンID（例: "Asia/Tokyo"）</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task SetTimezoneAsync(string timezone, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var timeZoneId = timezone;

            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            // タイムゾーンを保存
            await _configs.SetSaveConfig(ConfigKey.TimeZone, timeZoneId);

            var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");

            bool interactive = false;

            await td.SetTimezoneAsync(timeZoneId, interactive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to set timezone via D-Bus.");
            throw;
        }
    }

    /// <summary>
    /// NTPの有効/無効状態を取得する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns>NTPの有効/無効状態</returns>
    public async Task<bool> GetNTPConfigAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            //using var connection = new Connection(Address.System);
            //await connection.ConnectAsync();

            //var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");
            //bool ntpEnabled = await td.GetAsync<bool>("NTP");

            return _configs.GetConfig(ConfigKey.NtpEnable) == Constants.StrEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to get NTP config via D-Bus.");
            throw;
        }
    }

    public async Task<string> GetNTPServerAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            // null 許容型のため、null の場合は空文字列を返すことで CS8603 を回避
            return _configs.GetConfig(ConfigKey.NtpUrl) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to get NTP server via D-Bus.");
            throw;
        }
    }

    /// <summary>
    /// NTPの有効/無効を設定する
    /// </summary>
    /// <param name="enable">NTPの有効/無効状態</param>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task SetNTPAsync(bool enable, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            var td = connection.CreateProxy<ITimedate1>(DbusNames.timedate1, "/org/freedesktop/timedate1");

            bool interactive = false;

            await td.SetNTPAsync(enable, interactive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "failed to set NTP via D-Bus.");
            throw;
        }
    }


    /// <summary>
    /// systemd-timesyncdサービスを再起動する
    /// </summary>
    /// <param name="ct">キャンセル通知用トークン</param>
    /// <returns></returns>
    public async Task RestartTimesyncServiceAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            var systemd = connection.CreateProxy<ISystemd1Manager>(
                DbusNames.systemd1,
                "/org/freedesktop/systemd1"
            );

            // "replace" モードで再起動
            await systemd.RestartUnitAsync("systemd-timesyncd.service", "replace");
            _logger.LogInformation("systemd-timesyncを再起動しました");
        }
        catch (DBusException ex)
        {
            _logger.LogError(ex, "D-Bus経由でsystemd-timesyncの再起動に失敗しました");
            throw new InvalidOperationException("systemd-timesyncの再起動に失敗しました", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "systemd-timesyncの再起動中に予期しないエラーが発生しました");
            throw new InvalidOperationException("systemd-timesyncの再起動に失敗しました", ex);
        }
    }
}
