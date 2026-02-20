# ConfigsServiceClient 使用ガイド（Launcher 側）

GATEWAY_Launcher から gRPC 経由で GATEWAY_Core の ConfigsManager にアクセスする方法の実例集です。

## 基本的な使い方

### DI で ConfigsServiceClient を注入

```csharp
using GATEWAYCore;
using static GATEWAYCore.ConfigsService;

namespace GATEWAYLauncher.Components.Services
{
    public class ProxyService
    {
        private readonly ConfigsServiceClient _configs;

        public ProxyService(ConfigsServiceClient configs)
        {
            _configs = configs;
        }
    }
}
```

## 設定の読み取り

### 1. 文字列値として取得

```csharp
// FileVersion を取得
var response = await _configs.GetConfigAsync(ConfigKey.FileVersion);
if (response.Success)
{
    Console.WriteLine($"FileVersion: {response.Value}");
}
else
{
    Console.WriteLine($"エラー: {response.Error}");
}

// タイムゾーンを取得
var response = await _configs.GetConfigAsync(ConfigKey.TimeZone);
if (response.Success)
{
    Console.WriteLine($"TimeZone: {response.Value}");
}
```

### 2. 型付きで取得

```csharp
// uint 型で取得 (Hex 値)
var response = await _configs.GetConfigAsync<uint>(ConfigKey.Eth0Ip);
if (response.Success)
{
    Console.WriteLine($"Eth0Ip: 0x{response.TypedValue:X8}");
}

// int 型で取得 (Decimal 値)
var response = await _configs.GetConfigAsync<int>(ConfigKey.ComEthWaitEnable);
if (response.Success)
{
    Console.WriteLine($"ComEthWaitEnable: {response.TypedValue}ms");
}
```

### 3. 設定範囲を取得

```csharp
// ポート番号の範囲を取得
var (min, max, rangeValue) = await _configs.GetConfigRangeAsync<int>(ConfigKey.NetworkPortRange);
Console.WriteLine($"ポート範囲: {min} ～ {max}");
Console.WriteLine($"現在の値: {rangeValue}");

// IP アドレスの範囲を取得（Hex 値）
var (min, max, rangeValue) = await _configs.GetConfigRangeAsync<uint>(ConfigKey.Eth0Ip);
Console.WriteLine($"IP 範囲: 0x{min:X8} ～ 0x{max:X8}");
Console.WriteLine($"現在の IP: 0x{rangeValue:X8}");
```

## 設定の書き込み

### 1. 更新と保存を一度に実行

```csharp
// 文字列値を設定
var response = await _configs.SetSaveConfigAsync(ConfigKey.TimeZone, "Asia/Tokyo");
if (!response.Success)
{
    Console.WriteLine($"設定の更新に失敗: {response.Error}");
}
else
{
    Console.WriteLine("設定を更新して保存しました");
}

// Hex 値を設定（整数値を渡すと自動的に文字列に変換される）
var response = await _configs.SetSaveConfigAsync(ConfigKey.Eth0Ip, 0xC0A80101);
if (!response.Success)
{
    Console.WriteLine($"設定の更新に失敗: {response.Error}");
}

// Decimal 値を設定
var response = await _configs.SetSaveConfigAsync(ConfigKey.ComEthWaitEnable, 5000);
if (!response.Success)
{
    Console.WriteLine($"設定の更新に失敗: {response.Error}");
}
```

### 2. 複数の設定をまとめて更新（注意：現在の API では非推奨）

**注意**: 現在の gRPC API では、複数の設定を個別に更新してから一度に保存する機能は提供されていません。
各 `SetSaveConfigAsync` 呼び出しは即座にファイルに保存されます。

複数の設定を更新する場合は、バッチ API の実装を検討してください。

## 実践例

### 例1: プロキシ設定の範囲チェックと表示

```csharp
public class ProxyService
{
    private readonly ISnackbar _snackbar;
    private readonly ProxyModel _model;
    private readonly ConfigsServiceClient _configs;
    
    private int _portMinValue = 1;
    private int _portMaxValue = 65535;

    public ProxyService(ISnackbar snackbar, ProxyModel model, ConfigsServiceClient configs)
    {
        _snackbar = snackbar;
        _model = model;
        _configs = configs;
    }

    /// <summary>
    /// サーバーから設定範囲を取得してモデルに反映
    /// </summary>
    public async Task LoadFromServerAsync()
    {
        try
        {
            // ポート範囲情報を取得
            var (min, max, rangeValue) = await _configs.GetConfigRangeAsync<int>(ConfigKey.NetworkPortRange);
            
            // フィールドに保持してバリデーションで使用
            _portMinValue = min;
            _portMaxValue = max;
            
            _snackbar.Add($"ポート範囲: {min} ～ {max}", Severity.Info);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"ポート範囲情報の取得エラー: {ex.Message}", Severity.Warning);
        }

        // プロキシ設定を取得して表示
        try
        {
            var proxyTypeResponse = await _configs.GetConfigAsync(ConfigKey.ProxyType);
            if (proxyTypeResponse.Success)
            {
                _model.SelectedProxyType = proxyTypeResponse.Value;
            }

            var proxyServerResponse = await _configs.GetConfigAsync(ConfigKey.ProxyServer);
            if (proxyServerResponse.Success)
            {
                _model.ProxyServer = proxyServerResponse.Value;
            }

            var proxyPortResponse = await _configs.GetConfigAsync<int>(ConfigKey.ProxyPort);
            if (proxyPortResponse.Success)
            {
                _model.ProxyPort = proxyPortResponse.TypedValue.ToString();
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"プロキシ設定の取得エラー: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// 入力値のバリデーション（範囲チェック付き）
    /// </summary>
    public bool ValidateInputs(ProxyModel model)
    {
        if (model.SelectedProxyType == "None")
        {
            return true;
        }

        // 空欄チェック
        if (string.IsNullOrWhiteSpace(model.ProxyServer) || string.IsNullOrWhiteSpace(model.ProxyPort))
        {
            _snackbar.Add("値を入力してください", Severity.Error);
            return false;
        }

        // ポートの範囲チェック（サーバーから取得した範囲を使用）
        if (int.TryParse(model.ProxyPort, out var portValue))
        {
            if (portValue < _portMinValue || portValue > _portMaxValue)
            {
                _snackbar.Add($"ポート番号は {_portMinValue} ～ {_portMaxValue} の範囲で入力してください", 
                    Severity.Error);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// プロキシ設定の保存
    /// </summary>
    public async Task<bool> SaveProxySettings(ProxyModel model)
    {
        // バリデーション
        if (!ValidateInputs(model))
        {
            return false;
        }

        try
        {
            // プロキシタイプを保存
            var typeResponse = await _configs.SetSaveConfigAsync(
                ConfigKey.ProxyType, 
                model.SelectedProxyType);
            
            if (!typeResponse.Success)
            {
                _snackbar.Add($"プロキシタイプの保存に失敗: {typeResponse.Error}", Severity.Error);
                return false;
            }

            if (model.SelectedProxyType == "Manual")
            {
                // プロキシサーバーを保存
                var serverResponse = await _configs.SetSaveConfigAsync(
                    ConfigKey.ProxyServer, 
                    model.ProxyServer);
                
                if (!serverResponse.Success)
                {
                    _snackbar.Add($"プロキシサーバーの保存に失敗: {serverResponse.Error}", Severity.Error);
                    return false;
                }

                // プロキシポートを保存
                var portResponse = await _configs.SetSaveConfigAsync(
                    ConfigKey.ProxyPort, 
                    int.Parse(model.ProxyPort!));
                
                if (!portResponse.Success)
                {
                    _snackbar.Add($"プロキシポートの保存に失敗: {portResponse.Error}", Severity.Error);
                    return false;
                }
            }

            _snackbar.Add("プロキシ設定を保存しました", Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"プロキシ設定の保存に失敗: {ex.Message}", Severity.Error);
            return false;
        }
    }
}
```

### 例2: Blazor ページでの使用

```razor
@page "/proxy"
@layout MainLayout
@rendermode InteractiveServer
@inject MudBlazor.ISnackbar Snackbar
@inject Services.ProxyService ProxyService

@if (Model != null)
{
    <PageTitle>Proxy</PageTitle>

    <MudGrid>
        <MudItem xs="12">
            <MudRadioGroup T="string" @bind-Value="EditModel.SelectedProxyType">
                <MudItem xs="12">
                    <MudRadio T="string" Value="@("None")" Color="Color.Primary">
                        プロキシを使用しない
                    </MudRadio>
                </MudItem>
                <MudItem xs="12">
                    <MudRadio T="string" Value="@("Manual")" Color="Color.Primary">
                        プロキシを使用する
                    </MudRadio>
                </MudItem>
            </MudRadioGroup>
        </MudItem>

        <MudItem xs="2" Class="d-flex align-center">
            <MudText>プロキシサーバ：</MudText>
        </MudItem>
        <MudItem xs="4">
            <MudTextField @bind-Value="EditModel.ProxyServer" 
                        Variant="Variant.Outlined" 
                        Disabled="@(EditModel.SelectedProxyType == "None")" />
        </MudItem>
        
        <MudItem xs="1" Class="d-flex align-center">
            <MudText>ポート：</MudText>
        </MudItem>
        <MudItem xs="2">
            <MudTextField @bind-Value="EditModel.ProxyPort" 
                        Variant="Variant.Outlined" 
                        Disabled="@(EditModel.SelectedProxyType == "None")" />
        </MudItem>
    </MudGrid>

    <MudItem Class="d-flex justify-end align-center">
        <MudButton Color="Color.Primary" 
                   Variant="Variant.Filled" 
                   OnClick="OnProxySettings">
            適用
        </MudButton>
    </MudItem>
}
else
{
    <MudText>データを読み込み中...</MudText>
}

@code {
    private ProxyModel? Model;
    private ProxyModel EditModel { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // サーバーからデータをロード
        await ProxyService.LoadFromServerAsync();

        // 元データを取得し、編集用にクローンを作成
        Model = ProxyService.GetModel();
        EditModel = ProxyService.CloneModel(Model);
    }

    private async Task OnProxySettings()
    {
        var success = await ProxyService.SaveProxySettings(EditModel);
        
        if (success)
        {
            Snackbar.Add("プロキシ設定を適用しました", Severity.Success);
        }
    }
}
```

### 例3: ネットワーク設定の範囲チェック

```csharp
public class NetworkService
{
    private readonly ConfigsServiceClient _configs;
    private readonly ISnackbar _snackbar;

    public NetworkService(ConfigsServiceClient configs, ISnackbar snackbar)
    {
        _configs = configs;
        _snackbar = snackbar;
    }

    /// <summary>
    /// IP アドレスの範囲を取得して表示
    /// </summary>
    public async Task<(uint min, uint max)> GetIpRangeAsync()
    {
        try
        {
            var (min, max, currentValue) = await _configs.GetConfigRangeAsync<uint>(ConfigKey.Eth0Ip);
            
            _snackbar.Add($"IP 範囲: {FormatIp(min)} ～ {FormatIp(max)}", Severity.Info);
            _snackbar.Add($"現在の IP: {FormatIp(currentValue)}", Severity.Info);
            
            return (min, max);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"IP 範囲の取得エラー: {ex.Message}", Severity.Error);
            return (0, 0xFFFFFFFF);
        }
    }

    /// <summary>
    /// IP アドレスを更新
    /// </summary>
    public async Task<bool> UpdateIpAddressAsync(string ipAddress)
    {
        try
        {
            // IP アドレスを uint に変換
            var ipValue = ParseIpAddress(ipAddress);

            // 範囲チェック
            var (min, max) = await GetIpRangeAsync();
            if (ipValue < min || ipValue > max)
            {
                _snackbar.Add($"IP アドレスは {FormatIp(min)} ～ {FormatIp(max)} の範囲で入力してください", 
                    Severity.Error);
                return false;
            }

            // 設定を更新して保存
            var response = await _configs.SetSaveConfigAsync(ConfigKey.Eth0Ip, ipValue);
            if (!response.Success)
            {
                _snackbar.Add($"IP アドレスの更新に失敗: {response.Error}", Severity.Error);
                return false;
            }

            _snackbar.Add("IP アドレスを更新しました", Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"IP アドレスの更新エラー: {ex.Message}", Severity.Error);
            return false;
        }
    }

    private uint ParseIpAddress(string ipAddress)
    {
        var parts = ipAddress.Split('.');
        return (uint)((byte.Parse(parts[0]) << 24) |
                      (byte.Parse(parts[1]) << 16) |
                      (byte.Parse(parts[2]) << 8) |
                       byte.Parse(parts[3]));
    }

    private string FormatIp(uint ip)
    {
        return $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
    }
}
```

## エラーハンドリング

### gRPC 通信エラー

```csharp
try
{
    var response = await _configs.GetConfigAsync(ConfigKey.Eth0Ip);
    if (response.Success)
    {
        Console.WriteLine($"値: {response.Value}");
    }
    else
    {
        Console.WriteLine($"サーバーエラー: {response.Error}");
    }
}
catch (Grpc.Core.RpcException ex)
{
    Console.WriteLine($"gRPC 通信エラー: {ex.Status.Detail}");
}
catch (Exception ex)
{
    Console.WriteLine($"予期しないエラー: {ex.Message}");
}
```

### 設定更新時の検証エラー

```csharp
var response = await _configs.SetSaveConfigAsync(ConfigKey.Eth0Ip, "invalid_value");
if (!response.Success)
{
    // エラーメッセージには検証失敗の詳細が含まれる
    Console.WriteLine($"設定の検証に失敗: {response.Error}");
    // 例: "キー 'Eth0Ip' の検証エラー: 無効な16進数です: invalid_value"
}
```

## API 対応表

| Core 側 (ConfigsManager) | Launcher 側 (ConfigsServiceClient) | 説明 |
|-------------------------|-----------------------------------|------|
| `GetConfig(key)` | `await GetConfigAsync(key)` | 文字列として取得 |
| `GetConfig<T>(key)` | `await GetConfigAsync<T>(key)` | 型付きで取得 |
| `SetConfig(key, value)` | （未実装） | メモリのみ更新 |
| `SaveConfig()` | （未実装） | ファイルに保存 |
| `SetSaveConfig(key, value)` | `await SetSaveConfigAsync(key, value)` | 更新と保存を一度に |
| （未実装） | `await GetConfigRangeAsync<T>(key)` | 範囲情報を取得 |

## 注意事項

1. **非同期呼び出し**
   - すべての ConfigsServiceClient のメソッドは非同期です
   - 必ず `await` を使用してください

2. **gRPC 通信**
   - Launcher と Core 間は gRPC で通信します
   - ネットワークエラーや接続エラーに注意してください

3. **レスポンスの確認**
   - すべての応答には `Success` と `Error` プロパティがあります
   - 必ず `Success` を確認してから値を使用してください

4. **範囲チェック**
   - `GetConfigRangeAsync` を使用して、サーバー側の範囲情報を取得できます
   - 入力値のバリデーションに活用してください

5. **型変換**
   - `GetConfigAsync<T>()` は自動的に型変換を行います
   - Hex 値は `uint` 型で取得できます
   - Decimal 値は `int` 型で取得できます

6. **バッチ更新**
   - 現在の API では、複数の設定をメモリに保持してから一括保存する機能はありません
   - 各 `SetSaveConfigAsync` 呼び出しは即座にファイルに保存されます
