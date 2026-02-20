using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace GATEWAYCore.Infrastructure.Configs;

/// <summary>
/// 設定ファイル管理クラス
/// default、user、systemフォルダの設定ファイルを管理します
/// IConfig インターフェースでランタイムオブジェクトを管理
/// </summary>
public class ConfigsManager
{
    private readonly ILogger<ConfigsManager> _logger;
    private readonly string _defaultConfigsPath;
    private readonly string _systemConfigsPath;
    private readonly string _userConfigsPath;
    private Dictionary<string, Dictionary<string, string>> _configs = new();
    private Dictionary<string, IConfig> _runtimeConfigs = new();

    private const string FileVersion = "FileVersion";

    /// <summary>
    /// コンストラクタ（複数パス対応）
    /// </summary>
    public ConfigsManager(
        ILogger<ConfigsManager> logger,
        string configsBasePath)
        : this(
            logger,
            Path.Combine(configsBasePath, "default"),
            Path.Combine(configsBasePath, "system"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".GATEWAYCore", "Configs", "user"))
    {
    }

    /// <summary>
    /// コンストラクタ（個別パス指定）
    /// </summary>
    public ConfigsManager(
        ILogger<ConfigsManager> logger,
        string defaultConfigsPath,
        string systemConfigsPath,
        string userConfigsPath)
    {
        _logger = logger;
        _defaultConfigsPath = defaultConfigsPath;
        _systemConfigsPath = systemConfigsPath;
        _userConfigsPath = userConfigsPath;
        
        _logger.LogInformation($"ConfigsManager initialized:");
        _logger.LogInformation($"  Default: {_defaultConfigsPath}");
        _logger.LogInformation($"  System: {_systemConfigsPath}");
        _logger.LogInformation($"  User: {_userConfigsPath}");
    }

    /// <summary>
    /// 起動時に設定ファイルをメモリに展開し、バージョン管理とマージを行います
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("設定ファイル初期化を開始します");

            // ユーザー設定フォルダがなければ作成
            if (!Directory.Exists(_userConfigsPath))
            {
                Directory.CreateDirectory(_userConfigsPath);
                _logger.LogInformation($"ユーザー設定フォルダを作成しました: {_userConfigsPath}");
            }

            // ユーザー設定ファイルがなければ、デフォルトからコピー
            var userConfigsFile = Path.Combine(_userConfigsPath, "ConfigsUser.xml");
            var defaultConfigsFile = Path.Combine(_defaultConfigsPath, "ConfigsUser.xml");
            
            if (!File.Exists(userConfigsFile) && File.Exists(defaultConfigsFile))
            {
                try
                {
                    File.Copy(defaultConfigsFile, userConfigsFile);
                    _logger.LogInformation($"デフォルト設定ファイルをコピーしました: {defaultConfigsFile} -> {userConfigsFile}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"デフォルト設定ファイルのコピーに失敗しました: {ex.Message}");
                }
            }

            // systemフォルダのファイルを読み込み
            await LoadConfigsAsync("system", _systemConfigsPath);

            // userフォルダのファイルを読み込み、defaultとマージ
            await LoadAndMergeUserConfigsAsync();

            // ランタイムオブジェクト(IConfig)を初期化
            InitializeRuntimeConfigs();

            _logger.LogInformation("設定ファイル初期化が完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ファイル初期化エラーが発生しました");
            throw;
        }
    }

    /// <summary>
    /// ランタイム設定オブジェクト(IConfig)を初期化します
    /// </summary>
    private void InitializeRuntimeConfigs()
    {
        _runtimeConfigs.Clear();

        foreach (var definition in ConfigDefinitions.All)
        {
            var config = ConfigFactory.CreateConfig(definition);

            // XMLファイルから読み込んだ値がある場合、設定値を反映
            if (_configs.TryGetValue("user", out var userConfigs) &&
                userConfigs.TryGetValue(definition.Key, out var userValue))
            {
                var (success, errorMsg) = config.SetStringValue(userValue);
                if (!success)
                {
                    _logger.LogWarning($"設定値の設定に失敗しました: {errorMsg}");
                }
            }
            else if (_configs.TryGetValue("system", out var systemConfigs) &&
                     systemConfigs.TryGetValue(definition.Key, out var systemValue))
            {
                var (success, errorMsg) = config.SetStringValue(systemValue);
                if (!success)
                {
                    _logger.LogWarning($"設定値の設定に失敗しました: {errorMsg}");
                }
            }

            _runtimeConfigs[definition.Key] = config;
        }

        _logger.LogInformation($"ランタイム設定オブジェクトを初期化しました（{_runtimeConfigs.Count}個）");
    }

    /// <summary>
    /// ユーザー設定をdefaultからマージして読み込みます
    /// </summary>
    private async Task LoadAndMergeUserConfigsAsync()
    {
        _logger.LogDebug("LoadAndMergeUserConfigsAsync: START");
        var defaultConfigs = await LoadConfigsFromFileAsync("default", _defaultConfigsPath);
        _logger.LogDebug($"LoadAndMergeUserConfigsAsync: defaultConfigs = {(defaultConfigs != null ? defaultConfigs.Count + "個" : "null")}");
        
        var userConfigs = await LoadConfigsFromFileAsync("user", _userConfigsPath);
        _logger.LogDebug($"LoadAndMergeUserConfigsAsync: userConfigs = {(userConfigs != null ? userConfigs.Count + "個" : "null")}");

        if (defaultConfigs == null)
        {
            _logger.LogWarning("デフォルト設定ファイルが見つかりません");
            return;
        }

        // ユーザー設定が存在しない場合、デフォルトから生成
        if (userConfigs == null)
        {
            _logger.LogInformation("ユーザー設定ファイルが見つかりません。デフォルトから生成します");
            _logger.LogDebug("LoadAndMergeUserConfigsAsync: userConfigs == null, defaultConfigs をコピー");
            _configs["user"] = defaultConfigs;
            await SaveConfigsAsync("user", _userConfigsPath, defaultConfigs);
            return;
        }

        // バージョンを比較
        var defaultVersion = defaultConfigs.GetValueOrDefault(FileVersion, "0.0.0");
        var userVersion = userConfigs.GetValueOrDefault(FileVersion, "0.0.0");
        _logger.LogDebug($"LoadAndMergeUserConfigsAsync: defaultVersion={defaultVersion}, userVersion={userVersion}");

        if (defaultVersion != userVersion)
        {
            _logger.LogInformation($"設定ファイルのバージョンが異なります。 デフォルト: {defaultVersion}, ユーザー: {userVersion}");
            userConfigs = MergeConfigs(defaultConfigs, userConfigs);
            await SaveConfigsAsync("user", _userConfigsPath, userConfigs);
        }

        _logger.LogDebug("LoadAndMergeUserConfigsAsync: _configs[\"user\"] に userConfigs をセット");
        _configs["user"] = userConfigs;
        _logger.LogDebug("LoadAndMergeUserConfigsAsync: END");
    }

    /// <summary>
    /// デフォルト設定とユーザー設定をマージします
    /// デフォルト設定に新しいキーが追加された場合は追加し、デフォルト設定に削除されたキーはユーザー設定から削除します
    /// バージョン情報は除いてマージします
    /// </summary>
    private Dictionary<string, string> MergeConfigs(
        Dictionary<string, string> defaultConfigs,
        Dictionary<string, string> userConfigs)
    {
        var mergedConfigs = new Dictionary<string, string>(userConfigs);

        // デフォルト設定に新しいキーを追加
        foreach (var kvp in defaultConfigs)
        {
            // バージョン情報は常にデフォルトのものを使用
            if (kvp.Key == FileVersion)
            {
                mergedConfigs[kvp.Key] = kvp.Value;
                _logger.LogInformation($"バージョン情報を更新します: {kvp.Key} = {kvp.Value}");
            }
            else if (!mergedConfigs.ContainsKey(kvp.Key))
            {
                _logger.LogInformation($"新しい設定キーを追加します: {kvp.Key} = {kvp.Value}");
                mergedConfigs[kvp.Key] = kvp.Value;
            }
        }

        // デフォルト設定に削除されたキーはユーザー設定から削除
        var keysToRemove = mergedConfigs.Keys
            .Where(k => !defaultConfigs.ContainsKey(k))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _logger.LogInformation($"削除された設定キーを削除します: {key}");
            mergedConfigs.Remove(key);
        }

        return mergedConfigs;
    }

    /// <summary>
    /// 指定されたフォルダから設定ファイルを読み込みます
    /// </summary>
    private async Task LoadConfigsAsync(string folderName, string basePath)
    {
        var configs = await LoadConfigsFromFileAsync(folderName, basePath);
        if (configs != null)
        {
            _configs[folderName] = configs;
        }
    }

    /// <summary>
    /// 指定されたフォルダのConfigsUser.xmlファイルから設定を読み込みます
    /// </summary>
    private async Task<Dictionary<string, string>?> LoadConfigsFromFileAsync(string folderName, string basePath)
    {
        var filePath = Path.Combine(basePath, "ConfigsUser.xml");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"設定ファイルが見つかりません: {filePath}");
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var doc = XDocument.Parse(content);
            var configs = new Dictionary<string, string>();

            var dataElements = doc.Descendants("Data");
            foreach (var dataElement in dataElements)
            {
                var keyElement = dataElement.Element("Key")?.Value;
                var valueElement = dataElement.Element("Value")?.Value;

                if (!string.IsNullOrEmpty(keyElement))
                {
                    // 定義データから検証ルールを取得
                    var definition = ConfigDefinitions.GetByKey(keyElement);
                    if (definition != null)
                    {
                        var (isValid, errorMessage) = definition.ValidateValue(valueElement ?? "");
                        if (!isValid)
                        {
                            _logger.LogWarning($"設定値の検証エラー: {errorMessage}");
                        }
                    }

                    configs[keyElement] = valueElement ?? "";
                }
            }

            _logger.LogInformation($"{folderName}フォルダから {configs.Count} 個の設定を読み込みました: {filePath}");
            return configs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"設定ファイル読み込みエラー: {filePath}");
            return null;
        }
    }

    /// <summary>
    /// 設定をファイルに保存します
    /// </summary>
    private async Task SaveConfigsAsync(string folderName, string basePath, Dictionary<string, string> configs)
    {
        var filePath = Path.Combine(basePath, "ConfigsUser.xml");
        var directory = Path.GetDirectoryName(filePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        try
        {
            var xsiNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            var xsdNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema");

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("SystemData",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    new XElement("Config",
                        configs.Select(kvp => CreateDataElement(kvp.Key, kvp.Value))
                    )
                )
            );

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8))
            {
                await writer.WriteAsync(doc.ToString());
                await writer.FlushAsync();
            }

            _logger.LogInformation($"設定ファイルを保存しました: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"設定ファイル保存エラー: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// XMLのData要素を作成します
    /// </summary>
    private XElement CreateDataElement(string key, string value)
    {
        var definition = ConfigDefinitions.GetByKey(key);
        var itemName = definition?.Item ?? key;

        return new XElement("Data",
            new XElement("Item", itemName),
            new XElement("Key", key),
            new XElement("Value", value)
        );
    }

    /// <summary>
    /// キーから設定オブジェクトを取得
    /// </summary>
    public IConfig? GetRuntimeConfig(string key)
    {
        return _runtimeConfigs.TryGetValue(key, out var config) ? config : null;
    }

    /// <summary>
    /// 全ての設定オブジェクトを取得
    /// </summary>
    public IReadOnlyDictionary<string, IConfig> GetAllRuntimeConfigs()
    {
        return new Dictionary<string, IConfig>(_runtimeConfigs);
    }

    /// <summary>
    /// 指定された出力先の設定オブジェクトを取得
    /// </summary>
    public List<IConfig> GetRuntimeConfigsByKind(ConfigKind kind)
    {
        return _runtimeConfigs.Values
            .Where(s => s.Kind == kind)
            .ToList();
    }

    /// <summary>
    /// 設定値を更新
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateRuntimeConfigAsync(
        string key,
        object value)
    {
        if (!_runtimeConfigs.TryGetValue(key, out var config))
        {
            return (false, $"キー '{key}' が見つかりません");
        }

        // IConfig の SetValue メソッドで検証
        var (success, errorMessage) = config.SetValue(value);
        if (!success)
        {
            _logger.LogWarning($"設定値の更新に失敗しました: {errorMessage}");
            return (false, errorMessage);
        }

        _logger.LogInformation($"設定を更新しました: {key}");
        return (true, null);
    }

    /// <summary>
    /// 文字列値で設定を更新
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateRuntimeConfigStringAsync(
        string key,
        string value)
    {
        if (!_runtimeConfigs.TryGetValue(key, out var config))
        {
            return (false, $"キー '{key}' が見つかりません");
        }

        var (success, errorMessage) = config.SetStringValue(value);
        if (!success)
        {
            _logger.LogWarning($"設定値の更新に失敗しました: {errorMessage}");
            return (false, errorMessage);
        }

        _logger.LogInformation($"設定を更新しました: {key} = {value}");
        return (true, null);
    }

    /// <summary>
    /// 全ての設定を取得します
    /// </summary>
    public IReadOnlyDictionary<string, string>? GetAllConfigs(string folderName)
    {
        if (_configs.TryGetValue(folderName, out var configs))
        {
            return new Dictionary<string, string>(configs);
        }

        return null;
    }

    /// <summary>
    /// 指定されたキーの設定値を取得します（文字列値）
    /// user フォルダから優先的に取得し、なければ system フォルダから取得します
    /// </summary>
    public string? GetConfig(ConfigKey key)
    {
        var definition = ConfigDefinitions.GetByEnumKey(key);
        
        if (definition == null)
        {
            _logger.LogWarning($"設定定義が見つかりません: {key}");
            return string.Empty;
        }

        var keyString = definition.Key;

        // user フォルダから取得を試みる
        if (_configs.TryGetValue("user", out var userConfigs) &&
            userConfigs.TryGetValue(keyString, out var userValue))
        {
            return userValue;
        }

        // user に見つからなければ system フォルダから取得
        if (_configs.TryGetValue("system", out var systemConfigs) &&
            systemConfigs.TryGetValue(keyString, out var systemValue))
        {
            return systemValue;
        }

        // どちらにもなければ空文字列を返す
        return string.Empty;
    }

    /// <summary>
    /// 指定されたキーの設定値を型指定で取得します
    /// ConfigValueType に応じて適切な型で値を返します
    /// - Hex: uint または long
    /// - Decimal: int または long
    /// - String: string
    /// </summary>
    public T? GetConfig<T>(ConfigKey key) where T : notnull
    {
        var stringValue = GetConfig(key);
        if (string.IsNullOrEmpty(stringValue))
        {
            return default;
        }

        var definition = ConfigDefinitions.GetByEnumKey(key);
        if (definition == null)
        {
            _logger.LogWarning($"設定定義が見つかりません: {key}");
            return default;
        }

        try
        {
            return definition.ValueType switch
            {
                ConfigValueType.Hex => ConvertHexValue<T>(stringValue, key),
                ConfigValueType.Decimal => ConvertDecimalValue<T>(stringValue, key),
                ConfigValueType.String => (T)(object)stringValue,
                _ => default
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"設定値の変換に失敗しました。キー: {key}, 値: {stringValue}, エラー: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 16進数値を指定された型に変換
    /// </summary>
    private T? ConvertHexValue<T>(string hexValue, ConfigKey key) where T : notnull
    {
        if (typeof(T) == typeof(uint))
        {
            if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(hexValue.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var result))
                {
                    return (T)(object)result;
                }
            }
            else if (uint.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var result))
            {
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(long))
        {
            if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(hexValue.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var result))
                {
                    return (T)(object)result;
                }
            }
            else if (long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var result))
            {
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(int))
        {
            if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(hexValue.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var result))
                {
                    return (T)(object)result;
                }
            }
            else if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var result))
            {
                return (T)(object)result;
            }
        }

        throw new InvalidOperationException($"型 {typeof(T).Name} への変換をサポートしていません。キー: {key}");
    }

    /// <summary>
    /// 10進数値を指定された型に変換
    /// </summary>
    private T? ConvertDecimalValue<T>(string decimalValue, ConfigKey key) where T : notnull
    {
        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(decimalValue, out var result))
            {
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(long))
        {
            if (long.TryParse(decimalValue, out var result))
            {
                return (T)(object)result;
            }
        }
        else if (typeof(T) == typeof(uint))
        {
            if (uint.TryParse(decimalValue, out var result))
            {
                return (T)(object)result;
            }
        }

        throw new InvalidOperationException($"型 {typeof(T).Name} への変換をサポートしていません。キー: {key}");
    }

    /// <summary>
    /// メモリ上の設定値を更新（ユーザー設定のみ書き込み可能）
    /// ValueType に応じた値の検証と変換を自動実行
    /// Hex値はuint → "0x..." 形式に変換
    /// </summary>
    public (bool Success, string? ErrorMessage) SetConfig(ConfigKey key, object value)
    {
        var definition = ConfigDefinitions.GetByEnumKey(key);

        if (definition == null)
        {
            var errorMsg = $"設定定義が見つかりません: {key}";
            _logger.LogWarning(errorMsg);
            return (false, errorMsg);
        }

        // Hex値の場合、uint → "0x..." 形式に変換
        string stringValue;
        if (definition.ValueType == ConfigValueType.Hex && value is uint uintValue)
        {
            stringValue = "0x" + uintValue.ToString("X8");
        }
        else if (definition.ValueType == ConfigValueType.Hex && value is int intValue)
        {
            stringValue = "0x" + ((uint)intValue).ToString("X8");
        }
        else
        {
            stringValue = value?.ToString() ?? string.Empty;
        }

        return SetConfig(key, stringValue);
    }

    /// <summary>
    /// メモリ上の設定値を更新（ユーザー設定のみ書き込み可能）
    /// ValueType に応じた値の検証と形式変換を自動実行
    /// </summary>
    public (bool Success, string? ErrorMessage) SetConfig(ConfigKey key, string value)
    {
        var folderName = "user";
        var definition = ConfigDefinitions.GetByEnumKey(key);

        if (definition == null)
        {
            var errorMsg = $"設定定義が見つかりません: {key}";
            _logger.LogWarning(errorMsg);
            return (false, errorMsg);
        }

        // 値の型に応じた検証と変換
        var (isValid, errorMessage) = definition.ValidateValue(value);
        if (!isValid)
        {
            _logger.LogWarning($"設定値の検証に失敗しました: {errorMessage}");
            return (false, errorMessage);
        }

        var keyString = definition.Key;
        
        // ValueType に応じた形式に変換
        string convertedValue = value;
        try
        {
            if (definition.ValueType == ConfigValueType.Hex)
            {
                // Hex形式に統一 ("0xXXXXXXXX")
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // 既に "0x" で始まっている場合、大文字で統一
                    convertedValue = "0x" + value.Substring(2).ToUpper();
                }
                else if (uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var uintVal))
                {
                    // 16進数文字列として解釈
                    convertedValue = "0x" + uintVal.ToString("X8");
                }
                else if (uint.TryParse(value, out var decimalVal))
                {
                    // 10進数として解釈して16進数に変換
                    convertedValue = "0x" + decimalVal.ToString("X8");
                }
            }
            else if (definition.ValueType == ConfigValueType.Decimal)
            {
                // Decimal は10進数（既に10進数なはず）
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // 16進数から10進数に変換
                    var uintVal = uint.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);
                    convertedValue = uintVal.ToString();
                }
                else if (!uint.TryParse(value, out _))
                {
                    // 10進数として有効か確認
                    throw new FormatException($"'{value}' は有効な10進数ではありません");
                }
                // 既に10進数なら変換なし
            }
            // String型は変換なし
        }
        catch (Exception ex)
        {
            var errorMsg = $"値の形式変換に失敗しました: {ex.Message}";
            _logger.LogWarning(errorMsg);
            return (false, errorMsg);
        }

        if (!_configs.TryGetValue(folderName, out var configs))
        {
            configs = new Dictionary<string, string>();
            _configs[folderName] = configs;
        }

        configs[keyString] = convertedValue;
        _logger.LogInformation($"メモリ上の設定を更新しました: {keyString} = {convertedValue}");
        return (true, null);
    }

    /// <summary>
    /// メモリ上の設定値をユーザーファイルに保存
    /// </summary>
    public async Task SaveConfig()
    {
        var folderName = "user";
        var basePath = _userConfigsPath;

        if (_configs.TryGetValue(folderName, out var configs))
        {
            await SaveConfigsAsync(folderName, basePath, configs);
            _logger.LogInformation($"{folderName} フォルダの設定ファイルを保存しました");
        }
        else
        {
            _logger.LogWarning($"{folderName} フォルダの設定が見つかりません");
        }
    }

    /// <summary>
    /// メモリ上の設定値を更新してファイルに保存
    /// SetConfig + SaveConfig を一度に実行
    /// ValueType に応じた値の検証と変換を自動実行
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SetSaveConfig(ConfigKey key, object value)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        return await SetSaveConfig(key, stringValue);
    }

    /// <summary>
    /// メモリ上の設定値を更新してファイルに保存
    /// SetConfig + SaveConfig を一度に実行
    /// ValueType に応じた値の検証と変換を自動実行
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SetSaveConfig(ConfigKey key, string value)
    {
        // SetConfig で値の検証と更新を実行
        var (success, errorMessage) = SetConfig(key, value);
        if (!success)
        {
            return (false, errorMessage);
        }

        // 検証に成功したらファイルに保存
        try
        {
            await SaveConfig();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ファイルの保存に失敗しました");
            return (false, $"設定ファイルの保存に失敗しました: {ex.Message}");
        }
    }
}
