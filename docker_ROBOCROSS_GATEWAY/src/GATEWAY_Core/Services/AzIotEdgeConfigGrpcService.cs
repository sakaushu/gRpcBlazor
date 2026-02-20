using GATEWAYCore;
using GATEWAYCore.Application.UseCase;
using Grpc.Core;

/// <summary>
/// Azure IoT Edge設定gRPCサービス
/// </summary>
public sealed class AzIotEdgeConfigGrpcService : AzIotEdgeConfigService.AzIotEdgeConfigServiceBase
{
    private readonly ILogger<AzIotEdgeConfigGrpcService> _logger;

    private readonly AzIotEdgeConfigUseCase _useCase;

    public AzIotEdgeConfigGrpcService(
        ILogger<AzIotEdgeConfigGrpcService> logger,
        AzIotEdgeConfigUseCase useCase)
    {
        _logger = logger;
        _useCase = useCase;
    }

    /// <summary>
    /// Azure IoT Edgeの状態取得
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<AzIotEdgeStatusResponse> GetAzIotEdgeStatus(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetAzIotEdgeStatus called.");
            var status = await _useCase.GetAzIotEdgeStatusAsync();
            return new AzIotEdgeStatusResponse
            {
                AziotedgeStatus = { status }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Azure IoT Edge status.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get Azure IoT Edge status.", ex));
        }
    }

    /// <summary>
    /// Azure IoT Edgeモジュール情報の取得
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<AzIotEdgeModuleInfoResponse> GetAzIotEdgeModuleInfo(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetAzIotEdgeModuleInfo called.");
            var moduleList = await _useCase.GetAzIotEdgeModuleInfo();
            return new AzIotEdgeModuleInfoResponse
            {
                ModuleInfoList = { moduleList }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Azure IoT Edge module information.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get module information.", ex));
        }
    }

    /// <summary>
    /// Azure IoT Edge接続設定の取得
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<AzIotEdgeConnectionConfig> GetAzIotEdgeConnectionConfig(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("GetAzIotEdgeConnectionConfig called.");
            return await _useCase.GetIoTEdgeConnectionSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Azure IoT Edge connection configuration.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get connection configuration.", ex));
        }
    }

    /// <summary>
    /// Azure IoT Edge接続設定の適用
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task<AzIotEdgeConnectionConfig> ApplyAzIotEdgeConnectionConfig(AzIotEdgeConnectionConfig request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("ApplyAzIotEdgeConnectionConfig called.");
            // 設定の適用
            await _useCase.UpdateIoTEdgeConnectionAsync(request);

            // 適用後の設定を返す
            return await _useCase.GetIoTEdgeConnectionSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply Azure IoT Edge connection configuration.");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to apply connection configuration.", ex));
        }
    }
}
