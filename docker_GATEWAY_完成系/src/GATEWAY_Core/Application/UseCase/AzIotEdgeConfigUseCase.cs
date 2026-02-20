using GATEWAYCore;
using GATEWAYCore.Application.Abstractions;

namespace GATEWAYCore.Application.UseCase;

/// <summary>
/// Azure IoT Edge設定ユースケース
/// </summary>
public sealed class AzIotEdgeConfigUseCase
{
    private readonly IAzIotEdgeClient _azIotEdgeClient;

    public AzIotEdgeConfigUseCase(IAzIotEdgeClient azIotEdgeClient)
    {
        _azIotEdgeClient = azIotEdgeClient;
    }

    /// <summary>
    /// Azure IoT Edgeのステータスを取得
    /// </summary>
    /// <returns>IoT Edgeのステータス情報</returns>
    public async Task<Dictionary<string, string>> GetAzIotEdgeStatusAsync()
    {
        return await _azIotEdgeClient.GetStatusAsync();
    }

    /// <summary>
    /// Azure IoT Edgeモジュール情報を取得
    /// </summary>
    /// <returns>モジュール情報のリスト</returns>
    public async Task<List<AzIotEdgeModuleInfo>> GetAzIotEdgeModuleInfo()
    {
        return await _azIotEdgeClient.GetModuleInfoAsync();
    }

    /// <summary>
    /// Azure IoT Edgeの接続設定を取得
    /// </summary>
    /// <returns>接続設定情報</returns>
    public async Task<AzIotEdgeConnectionConfig> GetIoTEdgeConnectionSettingsAsync()
    {
        return await _azIotEdgeClient.GetConnectionConfigAsync();
    }

    /// <summary>
    /// Azure IoT Edgeの接続設定を更新
    /// </summary>
    /// <param name="request">新しい接続設定</param>
    public async Task UpdateIoTEdgeConnectionAsync(AzIotEdgeConnectionConfig request)
    {
        await _azIotEdgeClient.UpdateConnectionConfigAsync(request);
    }
}
