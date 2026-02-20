# ConfigsManager 使用ガイド

ConfigsManager を使用した設定の読み書き、保存の実例集です。

## 基本的な使い方

### DI で ConfigsManager を注入

```csharp
using GATEWAYCore.Infrastructure.Configs;

public class SetConfigGrpcService
{
    private readonly ConfigsManager _configs;

    public NetworkConfigGrpcService(
        ConfigsManager configs)
    {
        _configs = configs;
    }
}
```

## 設定の読み取り

### 1. 文字列値として取得

```csharp
// FileVersion を取得
var fileVersion = _configs.GetConfig(ConfigKey.FileVersion);
_logger.LogInformation("FileVersion: {FileVersion}", fileVersion ?? "N/A");

// タイムゾーンを取得
var timeZone = _configs.GetConfig(ConfigKey.TimeZone);
_logger.LogInformation("TimeZone: {TimeZone}", timeZone);
```

### 2. 型付きで取得

```csharp
// uint 型で取得 (Hex 値)
var ethIp = _configs.GetConfig<uint>(ConfigKey.Eth0Ip);
_logger.LogInformation("Eth0Ip: 0x{EthIp:X8}", ethIp);

// int 型で取得 (Decimal 値)
var waitTime = _configs.GetConfig<int>(ConfigKey.ComEthWaitEnable);
_logger.LogInformation("ComEthWaitEnable: {WaitTime}ms", waitTime);
```

## 設定の書き込み

### 1. メモリのみに書き込み（ファイルには保存しない）

```csharp
// 文字列値を設定
var (success, error) = _configs.SetConfig(ConfigKey.TimeZone, "Asia/Tokyo");
if (!success)
{
    _logger.LogError("設定の更新に失敗: {Error}", error);
}

// Hex 値を設定（整数値を渡すと自動的に文字列に変換される）
var (success, error) = _configs.SetConfig(ConfigKey.Eth0Ip, 0xC0A80101);
if (!success)
{
    _logger.LogError("設定の更新に失敗: {Error}", error);
}

// Decimal 値を設定
var (success, error) = _configs.SetConfig(ConfigKey.ComEthWaitEnable, 5000);
if (!success)
{
    _logger.LogError("設定の更新に失敗: {Error}", error);
}
```

### 2. 複数の設定をまとめて更新してから保存

```csharp
// 複数の設定を更新
var (success1, error1) = _configs.SetConfig(ConfigKey.Eth0Ip, 0xC0A80101);
var (success2, error2) = _configs.SetConfig(ConfigKey.Eth0Mask, 0xFFFFFF00);
var (success3, error3) = _configs.SetConfig(ConfigKey.Eth0Dhcp, 0);

// すべて成功したらファイルに保存
if (success1 && success2 && success3)
{
    await _configs.SaveConfig();
    _logger.LogInformation("設定をファイルに保存しました");
}
```

### 3. 更新と保存を一度に実行

```csharp
// SetConfig + SaveConfig を一度に実行
var (success, error) = await _configs.SetSaveConfig(ConfigKey.TimeZone, "Asia/Tokyo");
if (!success)
{
    _logger.LogError("設定の更新と保存に失敗: {Error}", error);
}
else
{
    _logger.LogInformation("設定を更新して保存しました");
}
```

## 実践例

### 例1: ネットワーク設定の更新

```csharp
public async Task<bool> UpdateNetworkConfigAsync(string ipAddress, string subnetMask)
{
    _logger.LogInformation("ネットワーク設定を更新します: IP={Ip}, Mask={Mask}", ipAddress, subnetMask);

    // IP アドレスを uint に変換
    var ipValue = ParseIpAddress(ipAddress);
    var maskValue = ParseIpAddress(subnetMask);

    // 設定を更新（メモリのみ）
    var (success1, error1) = _configs.SetConfig(ConfigKey.Eth0Ip, ipValue);
    var (success2, error2) = _configs.SetConfig(ConfigKey.Eth0Mask, maskValue);

    if (!success1 || !success2)
    {
        _logger.LogError("設定の更新に失敗: {Error1}, {Error2}", error1, error2);
        return false;
    }

    // ファイルに保存
    await _configs.SaveConfig();
    _logger.LogInformation("ネットワーク設定を保存しました");
    return true;
}

private uint ParseIpAddress(string ipAddress)
{
    var parts = ipAddress.Split('.');
    return (uint)((byte.Parse(parts[0]) << 24) |
                  (byte.Parse(parts[1]) << 16) |
                  (byte.Parse(parts[2]) << 8) |
                   byte.Parse(parts[3]));
}
```

### 例2: 設定値の増減

```csharp
public async Task<bool> IncrementWaitTimeAsync(ConfigKey key, int increment)
{
    // 現在の値を取得
    var currentValue = _configs.GetConfig<int>(key);
    _logger.LogInformation("現在の値: {CurrentValue}", currentValue);

    // 新しい値を計算
    var newValue = currentValue + increment;
    _logger.LogInformation("新しい値: {NewValue}", newValue);

    // 更新と保存を一度に実行
    var (success, error) = await _configs.SetSaveConfig(key, newValue);
    if (!success)
    {
        _logger.LogError("設定の更新に失敗: {Error}", error);
        return false;
    }

    _logger.LogInformation("設定を更新しました: {Key} = {NewValue}", key, newValue);
    return true;
}
```

## エラーハンドリング

### 設定更新時の検証エラー

```csharp
var (success, error) = await _configs.SetSaveConfig(ConfigKey.Eth0Ip, "invalid_value");
if (!success)
{
    // エラーメッセージには検証失敗の詳細が含まれる
    _logger.LogError("設定の検証に失敗: {Error}", error);
    // 例: "キー 'Eth0Ip' の検証エラー: 無効な16進数です: invalid_value"
}
```

### 型変換エラー

```csharp
try
{
    var value = _configs.GetConfig<int>(ConfigKey.TimeZone); // TimeZone は文字列型
}
catch (InvalidCastException ex)
{
    _logger.LogError(ex, "型変換エラー");
}
```

## 注意事項

1. **スレッドセーフティ**
   - ConfigsManager は Singleton で登録されているため、複数のリクエストから同時にアクセスされる可能性があります
   - 現在の実装ではロック機構がないため、競合状態に注意してください

2. **user 設定のみ変更可能**
   - SetConfig / SetSaveConfig は user フォルダの設定のみを更新します
   - system や default の設定は読み取り専用です

3. **型の自動変換**
   - SetConfig は object 型を受け取るため、整数値や文字列を渡すと自動的に文字列に変換されます
   - Hex 値は `0xC0A80101` のように整数で渡せます
   - Decimal 値も整数で渡せます

4. **バージョン管理**
   - FileVersion は default フォルダのバージョンで自動的に更新されます
   - ユーザーが FileVersion を直接変更することは推奨されません
