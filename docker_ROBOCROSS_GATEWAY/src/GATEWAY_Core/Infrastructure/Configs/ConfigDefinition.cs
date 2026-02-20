namespace GATEWAYCore.Infrastructure.Configs;

/// <summary>
/// 設定値のデータ型
/// </summary>
public enum ConfigValueType
{
    /// <summary>16進数値（0xで始まる）</summary>
    Hex,

    /// <summary>10進数値</summary>
    Decimal,

    /// <summary>文字列値</summary>
    String
}

/// <summary>
/// 設定の出力先（ConfigsUser.xml、ConfigsSystem.xmlの分け分け）
/// </summary>
public enum ConfigTarget
{
    /// <summary>両方のファイルに出力</summary>
    Both,

    /// <summary>ConfigsUser.xmlにのみ出力</summary>
    User,

    /// <summary>ConfigsSystem.xmlにのみ出力</summary>
    System
}

/// <summary>
/// 使用状況フラグ
/// </summary>
public enum ConfigUsed
{
    /// <summary>使用</summary>
    Use,

    /// <summary>未使用</summary>
    NotUse
}

/// <summary>
/// 設定ファイルで広く使用する定数を定義
/// </summary>
public class Constants
{
    /// <summary>1:有効</summary>
    public const string StrEnabled = "1";
    /// <summary>0:無効</summary>
    public const string StrDisabled = "0";
}

/// <summary>
/// 設定項目の定義（構造化データ）
/// Excelから出力される設定項目の型安全な定義
/// </summary>
public class ConfigDefinition
{
    /// <summary>設定項目の名前（日本語表記）</summary>
    public required string Item { get; init; }

    /// <summary>設定のキー</summary>
    public required string Key { get; init; }

    /// <summary>Excelでの行番号（順序管理用）</summary>
    public required ConfigKey EnumKey { get; init; }

    /// <summary>値（型に応じて long, string など）</summary>
    public required object Value { get; init; }

    /// <summary>値のデータ型</summary>
    public required ConfigValueType ValueType { get; init; }

    /// <summary>最小値（数値型の場合。HEXまたは10進数。文字列型の場合はnull）</summary>
    public long? ValueMin { get; init; }

    /// <summary>最大値（数値型の場合。HEXまたは10進数。文字列型の場合はnull）</summary>
    public long? ValueMax { get; init; }

    /// <summary>出力先（ConfigsUser.xml、ConfigsSystem.xmlなど）</summary>
    public required ConfigTarget Target { get; init; }

    /// <summary>使用状況</summary>
    public ConfigUsed Used { get; init; } = ConfigUsed.Use;

    /// <summary>インデックス番号</summary>
    public int Index { get; init; }

    /// <summary>
    /// 値が定義のバージョン確認用（FileVersionなど）かどうか
    /// </summary>
    public bool IsVersionKey => EnumKey == ConfigKey.FileVersion;

    /// <summary>
    /// デフォルト値を文字列として取得（互換性維持用）
    /// </summary>
    public string DefaultValue => GetValueAsString();

    /// <summary>
    /// 値を文字列に変換します
    /// </summary>
    public string GetValueAsString()
    {
        return ValueType switch
        {
            ConfigValueType.Hex => $"0x{Convert.ToInt64(Value):X}",
            ConfigValueType.Decimal => Convert.ToInt64(Value).ToString(),
            ConfigValueType.String => Value.ToString() ?? string.Empty,
            _ => Value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// 値をlong型で取得します（数値型用）
    /// </summary>
    public long GetValueAsLong()
    {
        return Convert.ToInt64(Value);
    }

    /// <summary>
    /// 設定値を検証します
    /// </summary>
    /// <param name="value">検証する値</param>
    /// <returns>検証結果とエラーメッセージ</returns>
    public (bool IsValid, string? ErrorMessage) ValidateValue(object value)
    {
        if (value == null)
        {
            return (false, $"キー '{Key}' の値が空です");
        }

        try
        {
            return ValueType switch
            {
                ConfigValueType.Hex => ValidateHexValue(value),
                ConfigValueType.Decimal => ValidateDecimalValue(value),
                ConfigValueType.String => ValidateStringValue(value),
                _ => (false, $"不明なデータ型: {ValueType}")
            };
        }
        catch (Exception ex)
        {
            return (false, $"キー '{Key}' の検証エラー: {ex.Message}");
        }
    }

    /// <summary>
    /// 16進数値を検証します
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateHexValue(object value)
    {
        long hexValue = value switch
        {
            long longValue => longValue,
            string stringValue => ParseHexString(stringValue),
            _ => throw new InvalidOperationException($"無効なHex値の型: {value.GetType()}")
        };

        if (ValueMin.HasValue && hexValue < ValueMin.Value)
        {
            return (false, $"キー '{Key}' の値が最小値より小さいです: 0x{hexValue:X} < 0x{ValueMin:X}");
        }

        if (ValueMax.HasValue && hexValue > ValueMax.Value)
        {
            return (false, $"キー '{Key}' の値が最大値より大きいです: 0x{hexValue:X} > 0x{ValueMax:X}");
        }

        return (true, null);
    }

    /// <summary>
    /// 10進数値を検証します
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateDecimalValue(object value)
    {
        long decimalValue = value switch
        {
            long longValue => longValue,
            string stringValue => long.Parse(stringValue),
            _ => throw new InvalidOperationException($"無効なDecimal値の型: {value.GetType()}")
        };

        if (ValueMin.HasValue && decimalValue < ValueMin.Value)
        {
            return (false, $"キー '{Key}' の値が最小値より小さいです: {decimalValue} < {ValueMin}");
        }

        if (ValueMax.HasValue && decimalValue > ValueMax.Value)
        {
            return (false, $"キー '{Key}' の値が最大値より大きいです: {decimalValue} > {ValueMax}");
        }

        return (true, null);
    }

    /// <summary>
    /// 文字列値を検証します
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateStringValue(object value)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return (false, $"キー '{Key}' の文字列値が空です");
        }

        return (true, null);
    }

    /// <summary>
    /// 16進数文字列をuint型に変換します
    /// 16進数として解釈できない場合は、10進数として解釈を試みます
    /// </summary>
    private static uint ParseHexString(string value)
    {
        string hexPart = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value.Substring(2)
            : value;

        // 16進数として解釈してみる
        if (uint.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
        {
            return hexValue;
        }

        // 16進数として失敗した場合は、10進数として解釈を試みる
        if (uint.TryParse(hexPart, out var decimalValue))
        {
            return decimalValue;
        }

        throw new InvalidOperationException($"無効な16進数または10進数です: {value}");
    }
}

/// <summary>
/// 全ての設定項目定義を管理するクラス
/// Excelから出力されるデータの構造を C# のコードで定義
/// </summary>
public static partial class ConfigDefinitions
{
    /// <summary>
    /// キーから設定定義を取得
    /// </summary>
    public static ConfigDefinition? GetByKey(string key)
    {
        return All.FirstOrDefault(s => s.Key == key);
    }

    /// <summary>
    /// ConfigKey enum から設定定義を取得（高速: インデックスアクセス）
    /// ConfigKey の値と配列インデックスが対応しているため O(1) で取得
    /// </summary>
    public static ConfigDefinition? GetByEnumKey(ConfigKey enumKey)
    {
        var index = (int)enumKey;
        if (index >= 0 && index < All.Count)
        {
            return All[index];
        }
        return null;
    }

    /// <summary>
    /// 指定された出力先の設定定義を取得
    /// </summary>
    public static IEnumerable<ConfigDefinition> GetByTarget(ConfigTarget target)
    {
        return All.Where(s =>
            s.Target == target ||
            (target == ConfigTarget.Both && s.Target == ConfigTarget.Both) ||
            (target == ConfigTarget.User && (s.Target == ConfigTarget.User || s.Target == ConfigTarget.Both)) ||
            (target == ConfigTarget.System && (s.Target == ConfigTarget.System || s.Target == ConfigTarget.Both))
        );
    }
}

/// <summary>
/// デフォルト設定オブジェクトセット
/// アプリケーション起動時に使用される IConfig インスタンスの初期値
/// </summary>
public static class DefaultRuntimeConfigs
{
    /// <summary>
    /// デフォルト実行時設定（遅延初期化）
    /// </summary>
    private static List<IConfig>? _defaultConfigs;

    /// <summary>
    /// デフォルト実行時設定を取得
    /// </summary>
    public static List<IConfig> GetDefaults()
    {
        _defaultConfigs ??= InitializeDefaults();
        return _defaultConfigs;
    }

    /// <summary>
    /// デフォルト設定を初期化
    /// ConfigDefinition から IConfig オブジェクトを生成
    /// </summary>
    private static List<IConfig> InitializeDefaults()
    {
        return ConfigDefinitions.All
            .Select(ConfigFactory.CreateConfig)
            .ToList();
    }
}
