namespace GATEWAYCore.Domain.Models;

/// <summary>
/// タイムゾーンの静的UTCオフセット情報
/// 主要なタイムゾーンのUTCオフセットを事前定義
/// </summary>
public static class TimeZoneOffsetData
{
    /// <summary>
    /// タイムゾーンIDとUTCオフセットのマッピング（UTCオフセット順にソート）
    /// キー: タイムゾーンID、値: UTCオフセット文字列
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> UtcOffsets = new Dictionary<string, string>
    {
        // UTC-11:00
        { "Pacific/Midway", "UTC-11:00" },
        { "Pacific/Pago_Pago", "UTC-11:00" },
        
        // UTC-10:00
        { "America/Honolulu", "UTC-10:00" },
        
        // UTC-09:00
        { "America/Anchorage", "UTC-09:00" },
        
        // UTC-08:00
        { "America/Los_Angeles", "UTC-08:00" },
        { "America/Vancouver", "UTC-08:00" },
        
        // UTC-07:00
        { "America/Denver", "UTC-07:00" },
        
        // UTC-06:00
        { "America/Chicago", "UTC-06:00" },
        { "America/Mexico_City", "UTC-06:00" },
        
        // UTC-05:00
        { "America/New_York", "UTC-05:00" },
        { "America/Toronto", "UTC-05:00" },
        { "America/Bogota", "UTC-05:00" },
        { "America/Lima", "UTC-05:00" },
        
        // UTC-04:00
        { "America/Santiago", "UTC-04:00" },
        { "America/Caracas", "UTC-04:00" },
        
        // UTC-03:00
        { "America/Sao_Paulo", "UTC-03:00" },
        { "America/Buenos_Aires", "UTC-03:00" },
        
        // UTC+00:00
        { "UTC", "UTC+00:00" },
        { "Etc/UTC", "UTC+00:00" },
        { "Etc/GMT", "UTC+00:00" },
        { "Europe/London", "UTC+00:00" },
        { "Europe/Dublin", "UTC+00:00" },
        { "Europe/Lisbon", "UTC+00:00" },
        
        // UTC+01:00
        { "Europe/Paris", "UTC+01:00" },
        { "Europe/Berlin", "UTC+01:00" },
        { "Europe/Rome", "UTC+01:00" },
        { "Europe/Madrid", "UTC+01:00" },
        { "Europe/Amsterdam", "UTC+01:00" },
        { "Europe/Brussels", "UTC+01:00" },
        { "Europe/Vienna", "UTC+01:00" },
        { "Europe/Prague", "UTC+01:00" },
        { "Europe/Warsaw", "UTC+01:00" },
        { "Europe/Stockholm", "UTC+01:00" },
        { "Africa/Lagos", "UTC+01:00" },
        { "Africa/Casablanca", "UTC+01:00" },
        
        // UTC+02:00
        { "Europe/Athens", "UTC+02:00" },
        { "Europe/Helsinki", "UTC+02:00" },
        { "Asia/Jerusalem", "UTC+02:00" },
        { "Asia/Beirut", "UTC+02:00" },
        { "Asia/Damascus", "UTC+02:00" },
        { "Africa/Cairo", "UTC+02:00" },
        { "Africa/Johannesburg", "UTC+02:00" },
        
        // UTC+03:00
        { "Europe/Istanbul", "UTC+03:00" },
        { "Europe/Moscow", "UTC+03:00" },
        { "Asia/Baghdad", "UTC+03:00" },
        { "Asia/Kuwait", "UTC+03:00" },
        { "Asia/Riyadh", "UTC+03:00" },
        { "Africa/Nairobi", "UTC+03:00" },
        
        // UTC+03:30
        { "Asia/Tehran", "UTC+03:30" },
        
        // UTC+04:00
        { "Asia/Dubai", "UTC+04:00" },
        { "Asia/Baku", "UTC+04:00" },
        
        // UTC+05:00
        { "Asia/Karachi", "UTC+05:00" },
        
        // UTC+05:30
        { "Asia/Kolkata", "UTC+05:30" },
        
        // UTC+05:45
        { "Asia/Kathmandu", "UTC+05:45" },
        
        // UTC+06:00
        { "Asia/Dhaka", "UTC+06:00" },
        
        // UTC+07:00
        { "Asia/Bangkok", "UTC+07:00" },
        { "Asia/Jakarta", "UTC+07:00" },
        { "Asia/Ho_Chi_Minh", "UTC+07:00" },
        
        // UTC+08:00
        { "Asia/Shanghai", "UTC+08:00" },
        { "Asia/Hong_Kong", "UTC+08:00" },
        { "Asia/Singapore", "UTC+08:00" },
        { "Asia/Taipei", "UTC+08:00" },
        { "Australia/Perth", "UTC+08:00" },
        
        // UTC+09:00
        { "Asia/Tokyo", "UTC+09:00" },
        { "Asia/Seoul", "UTC+09:00" },
        
        // UTC+09:30
        { "Australia/Adelaide", "UTC+09:30" },
        { "Australia/Darwin", "UTC+09:30" },
        
        // UTC+10:00
        { "Australia/Sydney", "UTC+10:00" },
        { "Australia/Melbourne", "UTC+10:00" },
        { "Australia/Brisbane", "UTC+10:00" },
        { "Pacific/Guam", "UTC+10:00" },
        { "Pacific/Port_Moresby", "UTC+10:00" },
        
        // UTC+12:00
        { "Pacific/Auckland", "UTC+12:00" },
        { "Pacific/Fiji", "UTC+12:00" },
        
        // UTC+13:00
        { "Pacific/Tongatapu", "UTC+13:00" },
        
        // UTC+14:00
        { "Pacific/Kiritimati", "UTC+14:00" },
    };

    /// <summary>
    /// タイムゾーンIDからUTCオフセット文字列を取得
    /// </summary>
    /// <param name="timeZoneId">タイムゾーンID</param>
    /// <returns>UTCオフセット文字列（例: "UTC+09:00"）。見つからない場合は"UTC+00:00"</returns>
    public static string GetUtcOffsetString(string timeZoneId)
    {
        if (UtcOffsets.TryGetValue(timeZoneId, out var offset))
        {
            return offset;
        }

        // デフォルトはUTC+00:00
        return "UTC+00:00";
    }

    /// <summary>
    /// すべてのタイムゾーンIDのリストを取得
    /// </summary>
    /// <returns>タイムゾーンIDのリスト</returns>
    public static IEnumerable<string> GetAllTimeZoneIds()
    {
        return UtcOffsets.Keys;
    }

    /// <summary>
    /// すべてのUTCオフセット文字列のリストを取得
    /// </summary>
    /// <returns>UTCオフセット文字列のリスト（重複あり）</returns>
    public static IEnumerable<string> GetAllUtcOffsetStrings()
    {
        return UtcOffsets.Values;
    }
}
