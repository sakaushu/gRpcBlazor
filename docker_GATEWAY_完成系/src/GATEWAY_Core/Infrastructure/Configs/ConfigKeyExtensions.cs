namespace GATEWAYCore.Infrastructure.Configs;

public static class ConfigKeyExtensions
{
    /// <summary>
    /// キー名をそのまま文字列として取得します。
    /// </summary>
    public static string GetKeyName(this ConfigKey key) => key.ToString();

    /// <summary>
    /// SettingsManager から指定キーのランタイム設定を取得します。
    /// </summary>
    public static IConfig? GetConfig(this ConfigsManager manager, ConfigKey key)
        => manager.GetRuntimeConfig(key.GetKeyName());
}
