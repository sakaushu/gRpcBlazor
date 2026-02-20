namespace GATEWAYCore.Infrastructure.Configs;

/// <summary>
/// 設定項目の基本インターフェース
/// ランタイムで保持される設定オブジェクト
/// </summary>
public interface IConfig
{
    /// <summary>設定項目の名前（日本語表記）</summary>
    string Item { get; }

    /// <summary>設定のキー</summary>
    string Key { get; }

    /// <summary>Excelでの行番号</summary>
    int Index { get; }

    /// <summary>使用状況</summary>
    bool Use { get; }

    /// <summary>出力先（ConfigsUser.xml、ConfigsSystem.xmlなど）</summary>
    ConfigKind Kind { get; }

    /// <summary>
    /// 値を取得（オブジェクト型）
    /// </summary>
    object GetValue();

    /// <summary>
    /// 値を設定（オブジェクト型）
    /// </summary>
    /// <param name="value">設定する値</param>
    /// <returns>設定結果と検証エラーメッセージ</returns>
    (bool Success, string? ErrorMessage) SetValue(object value);

    /// <summary>
    /// 文字列値を取得
    /// </summary>
    string GetStringValue();

    /// <summary>
    /// 文字列値を設定
    /// </summary>
    (bool Success, string? ErrorMessage) SetStringValue(string value);
}

/// <summary>
/// 設定の出力先種別
/// （ConfigTarget の別名、より使いやすい名前）
/// </summary>
public enum ConfigKind
{
    /// <summary>両方のファイルに出力</summary>
    Both,

    /// <summary>ConfigsUser.xmlにのみ出力</summary>
    User,

    /// <summary>ConfigsSystem.xmlにのみ出力</summary>
    System
}

/// <summary>
/// 文字列値の設定
/// </summary>
public class ConfigString : IConfig
{
    private string _value;
    private readonly ConfigDefinition? _definition;

    public string Item { get; init; } = "";
    public string Key { get; init; } = "";
    public int Index { get; init; }
    public bool Use { get; init; }
    public ConfigKind Kind { get; init; }

    /// <summary>設定値</summary>
    public string Value
    {
        get => _value;
        set => SetStringValue(value);
    }

    public ConfigString() : this(null)
    {
    }

    /// <summary>
    /// 定義データから初期化
    /// </summary>
    /// <param name="definition">設定定義</param>
    internal ConfigString(ConfigDefinition? definition)
    {
        _definition = definition;
        _value = definition?.DefaultValue ?? "";

        if (definition != null)
        {
            Item = definition.Item;
            Key = definition.Key;
            Index = definition.Index;
            Use = definition.Used == ConfigUsed.Use;
            Kind = ConvertTarget(definition.Target);
        }
    }

    /// <summary>
    /// 設定値を取得します
    /// </summary>
    /// <returns></returns>
    public object GetValue() => _value;

    public (bool Success, string? ErrorMessage) SetValue(object value)
    {
        if (value is string strValue)
            return SetStringValue(strValue);

        return (false, $"キー '{Key}' は文字列値です。型が不一致です: {value?.GetType().Name ?? "null"}");
    }

    public string GetStringValue() => _value;

    public (bool Success, string? ErrorMessage) SetStringValue(string value)
    {
        if (_definition != null)
        {
            var (isValid, errorMessage) = _definition.ValidateValue(value);
            if (!isValid)
            {
                return (false, errorMessage);
            }
        }

        _value = value;
        return (true, null);
    }

    private static ConfigKind ConvertTarget(ConfigTarget target) => target switch
    {
        ConfigTarget.Both => ConfigKind.Both,
        ConfigTarget.User => ConfigKind.User,
        ConfigTarget.System => ConfigKind.System,
        _ => ConfigKind.Both
    };
}

/// <summary>
/// 16進数値（UInt32）の設定
/// </summary>
public class ConfigUInt32 : IConfig
{
    private uint _value;
    private readonly ConfigDefinition? _definition;

    public string Item { get; init; } = "";
    public string Key { get; init; } = "";
    public int Index { get; init; }
    public bool Use { get; init; }
    public ConfigKind Kind { get; init; }
    public uint Min { get; init; }
    public uint Max { get; init; } = uint.MaxValue;

    /// <summary>設定値</summary>
    public uint Value
    {
        get => _value;
        set => SetValue(value);
    }

    public ConfigUInt32() : this(null)
    {
    }

    /// <summary>
    /// 定義データから初期化
    /// </summary>
    internal ConfigUInt32(ConfigDefinition? definition)
    {
        _definition = definition;

        if (definition != null)
        {
            Item = definition.Item;
            Key = definition.Key;
            Index = definition.Index;
            Use = definition.Used == ConfigUsed.Use;
            Kind = ConvertTarget(definition.Target);
            Min = (uint)(definition.ValueMin ?? 0);
            Max = (uint)(definition.ValueMax ?? uint.MaxValue);

            // デフォルト値をパース
            if (definition.DefaultValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                _value = uint.Parse(definition.DefaultValue.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            else if (uint.TryParse(definition.DefaultValue, out var val))
            {
                _value = val;
            }
        }
    }

    public object GetValue() => _value;

    public (bool Success, string? ErrorMessage) SetValue(object value)
    {
        if (value is uint uintValue)
        {
            return SetValue(uintValue);
        }

        if (value is int intValue && intValue >= 0)
        {
            return SetValue((uint)intValue);
        }

        if (value is string strValue)
        {
            if (strValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(strValue.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
                {
                    return SetValue(hexValue);
                }
            }
            else if (uint.TryParse(strValue, out var decValue))
            {
                return SetValue(decValue);
            }

            return (false, $"キー '{Key}' の値が無効な16進数または10進数です: {value}");
        }

        return (false, $"キー '{Key}' は16進数値です。型が不一致です: {value?.GetType().Name ?? "null"}");
    }

    public string GetStringValue() => $"0x{_value:X8}";

    public (bool Success, string? ErrorMessage) SetStringValue(string value)
    {
        if (_definition != null)
        {
            var (isValid, errorMessage) = _definition.ValidateValue(value);
            if (!isValid)
            {
                return (false, errorMessage);
            }
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
            {
                if (hexValue < Min)
                    return (false, $"キー '{Key}' の値が最小値より小さいです: {value} < 0x{Min:X8}");
                if (hexValue > Max)
                    return (false, $"キー '{Key}' の値が最大値より大きいです: {value} > 0x{Max:X8}");

                _value = hexValue;
                return (true, null);
            }
        }

        return (false, $"キー '{Key}' の値が無効な16進数です: {value}");
    }

    private (bool Success, string? ErrorMessage) SetValue(uint value)
    {
        if (value < Min)
            return (false, $"キー '{Key}' の値が最小値より小さいです: 0x{value:X8} < 0x{Min:X8}");
        if (value > Max)
            return (false, $"キー '{Key}' の値が最大値より大きいです: 0x{value:X8} > 0x{Max:X8}");

        _value = value;
        return (true, null);
    }

    private static ConfigKind ConvertTarget(ConfigTarget target) => target switch
    {
        ConfigTarget.Both => ConfigKind.Both,
        ConfigTarget.User => ConfigKind.User,
        ConfigTarget.System => ConfigKind.System,
        _ => ConfigKind.Both
    };
}

/// <summary>
/// 10進数値（Int64）の設定
/// </summary>
public class ConfigInt64 : IConfig
{
    private long _value;
    private readonly ConfigDefinition? _definition;

    public string Item { get; init; } = "";
    public string Key { get; init; } = "";
    public int Index { get; init; }
    public bool Use { get; init; }
    public ConfigKind Kind { get; init; }
    public long Min { get; init; }
    public long Max { get; init; } = long.MaxValue;

    /// <summary>設定値</summary>
    public long Value
    {
        get => _value;
        set => SetValue(value);
    }

    public ConfigInt64() : this(null)
    {
    }

    /// <summary>
    /// 定義データから初期化
    /// </summary>
    internal ConfigInt64(ConfigDefinition? definition)
    {
        _definition = definition;

        if (definition != null)
        {
            Item = definition.Item;
            Key = definition.Key;
            Index = definition.Index;
            Use = definition.Used == ConfigUsed.Use;
            Kind = ConvertTarget(definition.Target);
            Min = definition.ValueMin ?? 0;
            Max = definition.ValueMax ?? long.MaxValue;

            if (long.TryParse(definition.DefaultValue, out var val))
            {
                _value = val;
            }
        }
    }

    public object GetValue() => _value;

    public (bool Success, string? ErrorMessage) SetValue(object value)
    {
        if (value is long longValue)
        {
            return SetValue(longValue);
        }

        if (value is int intValue)
        {
            return SetValue(intValue);
        }

        if (value is string strValue)
        {
            if (long.TryParse(strValue, out var parsedValue))
            {
                return SetValue(parsedValue);
            }

            return (false, $"キー '{Key}' の値が無効な10進数です: {value}");
        }

        return (false, $"キー '{Key}' は10進数値です。型が不一致です: {value?.GetType().Name ?? "null"}");
    }

    public string GetStringValue() => _value.ToString();

    public (bool Success, string? ErrorMessage) SetStringValue(string value)
    {
        if (_definition != null)
        {
            var (isValid, errorMessage) = _definition.ValidateValue(value);
            if (!isValid)
            {
                return (false, errorMessage);
            }
        }

        if (long.TryParse(value, out var parsedValue))
        {
            if (parsedValue < Min)
                return (false, $"キー '{Key}' の値が最小値より小さいです: {value} < {Min}");
            if (parsedValue > Max)
                return (false, $"キー '{Key}' の値が最大値より大きいです: {value} > {Max}");

            _value = parsedValue;
            return (true, null);
        }

        return (false, $"キー '{Key}' の値が無効な10進数です: {value}");
    }

    private (bool Success, string? ErrorMessage) SetValue(long value)
    {
        if (value < Min)
            return (false, $"キー '{Key}' の値が最小値より小さいです: {value} < {Min}");
        if (value > Max)
            return (false, $"キー '{Key}' の値が最大値より大きいです: {value} > {Max}");

        _value = value;
        return (true, null);
    }

    private static ConfigKind ConvertTarget(ConfigTarget target) => target switch
    {
        ConfigTarget.Both => ConfigKind.Both,
        ConfigTarget.User => ConfigKind.User,
        ConfigTarget.System => ConfigKind.System,
        _ => ConfigKind.Both
    };
}

/// <summary>
/// 設定ファクトリ
/// ConfigDefinitionから適切なIConfig実装を生成
/// </summary>
public static class ConfigFactory
{
    /// <summary>
    /// 定義データから設定オブジェクトを生成
    /// </summary>
    public static IConfig CreateConfig(ConfigDefinition definition)
    {
        return definition.ValueType switch
        {
            ConfigValueType.String => new ConfigString(definition),
            ConfigValueType.Hex => new ConfigUInt32(definition),
            ConfigValueType.Decimal => new ConfigInt64(definition),
            _ => throw new ArgumentException($"不明なデータ型: {definition.ValueType}")
        };
    }

    /// <summary>
    /// 全ての設定定義から設定オブジェクトを生成
    /// </summary>
    public static List<IConfig> CreateAllConfigs()
    {
        return ConfigDefinitions.All
            .Select(CreateConfig)
            .ToList();
    }

    /// <summary>
    /// 指定された出力先の設定オブジェクトを生成
    /// </summary>
    public static List<IConfig> CreateConfigsByKind(ConfigKind kind)
    {
        var target = kind switch
        {
            ConfigKind.Both => ConfigTarget.Both,
            ConfigKind.User => ConfigTarget.User,
            ConfigKind.System => ConfigTarget.System,
            _ => ConfigTarget.Both
        };

        return ConfigDefinitions.GetByTarget(target)
            .Select(CreateConfig)
            .ToList();
    }
}

/// <summary>
/// 設定レジストリ
/// アプリケーション全体で使用する IConfig インスタンスをキャッシュ・管理
/// </summary>
public static class ConfigRegistry
{
    private static List<IConfig>? _allConfigs;
    private static Dictionary<string, IConfig>? _configsByKey;
    private static List<IConfig>? _userConfigs;
    private static List<IConfig>? _systemConfigs;
    private static List<IConfig>? _bothConfigs;

    /// <summary>
    /// 全ての設定を取得（キャッシュ付き）
    /// </summary>
    public static List<IConfig> GetAllConfigs()
    {
        _allConfigs ??= ConfigFactory.CreateAllConfigs();
        return _allConfigs;
    }

    /// <summary>
    /// キーで設定を検索（高速）
    /// </summary>
    public static IConfig? GetConfigByKey(string key)
    {
        _configsByKey ??= GetAllConfigs().ToDictionary(s => s.Key);
        return _configsByKey.TryGetValue(key, out var config) ? config : null;
    }

    /// <summary>
    /// ユーザー設定のみ取得（キャッシュ付き）
    /// </summary>
    public static List<IConfig> GetUserConfigs()
    {
        _userConfigs ??= GetAllConfigs().Where(s => s.Kind == ConfigKind.User).ToList();
        return _userConfigs;
    }

    /// <summary>
    /// システム設定のみ取得（キャッシュ付き）
    /// </summary>
    public static List<IConfig> GetSystemConfigs()
    {
        _systemConfigs ??= GetAllConfigs().Where(s => s.Kind == ConfigKind.System).ToList();
        return _systemConfigs;
    }

    /// <summary>
    /// 両方に出力される設定を取得（キャッシュ付き）
    /// </summary>
    public static List<IConfig> GetBothConfigs()
    {
        _bothConfigs ??= GetAllConfigs().Where(s => s.Kind == ConfigKind.Both).ToList();
        return _bothConfigs;
    }

    /// <summary>
    /// キャッシュをクリア（テスト時やリロード時に使用）
    /// </summary>
    public static void ClearCache()
    {
        _allConfigs = null;
        _configsByKey = null;
        _userConfigs = null;
        _systemConfigs = null;
        _bothConfigs = null;
    }

    /// <summary>
    /// カウント取得
    /// </summary>
    public static int Count => GetAllConfigs().Count;

    /// <summary>
    /// 種別ごとのカウント
    /// </summary>
    public static (int Total, int User, int System, int Both) GetCounts()
    {
        var all = GetAllConfigs();
        return (
            all.Count,
            GetUserConfigs().Count,
            GetSystemConfigs().Count,
            GetBothConfigs().Count
        );
    }
}
