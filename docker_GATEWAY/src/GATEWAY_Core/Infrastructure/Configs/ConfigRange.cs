namespace GATEWAYCore.Infrastructure.Configs;

/// <summary>
/// 設定値の範囲情報と現在の値
/// </summary>
public record ConfigRange
{
    /// <summary>最小値（数値型の場合）</summary>
    public long? Min { get; init; }

    /// <summary>最大値（数値型の場合）</summary>
    public long? Max { get; init; }

    /// <summary>値のデータ型</summary>
    public ConfigValueType ValueType { get; init; }

    /// <summary>現在の設定値</summary>
    public string? Value { get; init; }

    /// <summary>
    /// 範囲が定義されているかどうか
    /// </summary>
    public bool HasRange => Min.HasValue || Max.HasValue;

    /// <summary>
    /// 値が範囲内かどうかを検証
    /// </summary>
    public bool IsInRange(long value)
    {
        if (Min.HasValue && value < Min.Value)
            return false;

        if (Max.HasValue && value > Max.Value)
            return false;

        return true;
    }

    /// <summary>
    /// 範囲を文字列で取得
    /// </summary>
    public string GetRangeString()
    {
        return ValueType switch
        {
            ConfigValueType.Hex => $"0x{Min:X} - 0x{Max:X}",
            ConfigValueType.Decimal => $"{Min} - {Max}",
            ConfigValueType.String => "文字列（範囲なし）",
            _ => "不明"
        };
    }
}
