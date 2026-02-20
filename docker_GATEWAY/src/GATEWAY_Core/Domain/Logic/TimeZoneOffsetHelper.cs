using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GATEWAYCore.Domain.Logic;

/// <summary>
/// タイムゾーンオフセット情報を取得するヘルパークラス
/// CSVファイルからタイムゾーン情報を読み込む
/// </summary>
public static class TimeZoneOffsetHelper
{
    // CSVファイルパスを環境変数から取得、なければデフォルトパスを使用
    private static readonly string CsvFilePath = Environment.GetEnvironmentVariable("TIMEZONE_CSV_PATH")
        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "system", "timezones.csv");

    // タイムゾーンデータのキャッシュ
    private static readonly Lazy<Dictionary<string, string>> TimeZoneData = new(() => LoadTimeZoneData());

    /// <summary>
    /// CSVファイルからタイムゾーンデータを読み込む
    /// </summary>
    /// <returns>タイムゾーンIDとUTCオフセットのマッピング</returns>
    private static Dictionary<string, string> LoadTimeZoneData()
    {
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(CsvFilePath))
            {
                throw new FileNotFoundException($"Timezone CSV file not found: {CsvFilePath}");
            }

            var lines = File.ReadAllLines(CsvFilePath);

            // ヘッダー行をスキップ
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',', 2);
                if (parts.Length == 2)
                {
                    var timezoneId = parts[0].Trim();
                    var utcOffset = parts[1].Trim();

                    // CSVファイルは既にフィルタリング済み
                    if (!string.IsNullOrWhiteSpace(timezoneId) && !string.IsNullOrWhiteSpace(utcOffset))
                    {
                        data[timezoneId] = utcOffset;
                    }
                }
            }

            if (data.Count == 0)
            {
                throw new InvalidOperationException($"No valid timezone data found in: {CsvFilePath}");
            }

            return data;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error loading timezone data from CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// UTCオフセット文字列を分単位の数値に変換（ソート用）
    /// </summary>
    /// <param name="utcOffset">UTCオフセット文字列（例: "UTC+09:00"）</param>
    /// <returns>分単位のオフセット（例: 540）</returns>
    private static int UtcOffsetToMinutes(string utcOffset)
    {
        try
        {
            // UTC±HH:MM 形式を想定（例: UTC+09:00, UTC-05:00）
            if (string.IsNullOrWhiteSpace(utcOffset) || !utcOffset.StartsWith("UTC") || utcOffset.Length < 9)
                return 0;

            var offsetPart = utcOffset[3..];
            var sign = offsetPart[0] == '+' ? 1 : -1;
            var hours = int.Parse(offsetPart[1..3]);
            var minutes = int.Parse(offsetPart[4..6]);

            return sign * (hours * 60 + minutes);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// タイムゾーンIDからUTCオフセット文字列を取得
    /// </summary>
    /// <param name="timeZoneId">タイムゾーンID</param>
    /// <returns>UTCオフセット文字列（例: "UTC+09:00"）。見つからない場合は"UTC+00:00"</returns>
    public static string GetUtcOffsetString(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return "UTC+00:00";

        try
        {
            return TimeZoneData.Value.TryGetValue(timeZoneId, out var offset)
                ? offset
                : "UTC+00:00";
        }
        catch (Exception)
        {
            // エラー時はデフォルトのUTC+00:00を返す
            return "UTC+00:00";
        }
    }

    /// <summary>
    /// すべてのタイムゾーンIDのリストを取得（UTCオフセット順にソート済み）
    /// </summary>
    /// <returns>タイムゾーンIDのリスト（UTCオフセット順）</returns>
    public static IEnumerable<string> GetAllTimeZoneIds()
    {
        try
        {
            // UTCオフセット順、次にタイムゾーンID順にソート
            // CSVファイルは既に正規タイムゾーンのみに絞られている
            return TimeZoneData.Value.Keys
                .OrderBy(tz => UtcOffsetToMinutes(TimeZoneData.Value[tz]))
                .ThenBy(tz => tz)
                .ToList();
        }
        catch (Exception)
        {
            // エラー時は空のリストを返す
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// すべてのUTCオフセット文字列のリストを取得
    /// </summary>
    /// <returns>UTCオフセット文字列のリスト（重複あり）</returns>
    public static IEnumerable<string> GetAllUtcOffsetStrings()
    {
        try
        {
            return TimeZoneData.Value.Values.ToList();
        }
        catch (Exception)
        {
            // エラー時は空のリストを返す
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// ユニークなUTCオフセット文字列のリストを取得（ソート済み）
    /// </summary>
    /// <returns>UTCオフセット文字列のリスト（重複なし、ソート済み）</returns>
    public static IEnumerable<string> GetUniqueUtcOffsetStrings()
    {
        try
        {
            // UTCオフセットを数値順にソート
            return TimeZoneData.Value.Values
                .Distinct()
                .OrderBy(x => UtcOffsetToMinutes(x))
                .ToList();
        }
        catch (Exception)
        {
            // エラー時は空のリストを返す
            return Enumerable.Empty<string>();
        }
    }
}
