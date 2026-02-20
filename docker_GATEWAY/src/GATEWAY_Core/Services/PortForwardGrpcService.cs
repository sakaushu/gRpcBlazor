using Grpc.Core;
using GATEWAYCore.Application.UseCase;
using GATEWAYCore.Domain.Models;

namespace GATEWAYCore.Service;

/// <summary>
/// ポートフォワーディング設定のgRPCサービス
/// </summary>
public sealed class PortForwardGrpcService : PortForwardConfigService.PortForwardConfigServiceBase
{
    private readonly PortForwardUseCase _useCase;
    private readonly ILogger<PortForwardGrpcService> _logger;

    public PortForwardGrpcService(PortForwardUseCase useCase, ILogger<PortForwardGrpcService> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// ポートフォワーディング設定を適用
    /// </summary>
    /// <param name="request">適用するポートフォワーディングルールのリクエスト</param>
    /// <param name="context">gRPCのサーバーコールコンテキスト</param>
    /// <returns>適用結果のレスポンス</returns>
    public override async Task<ApplyPortForwardsResponse> ApplyPortForwards(
        ApplyPortForwardsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Received ApplyPortForwards request with {Count} rules", request.Rules.Count);

            // gRPC メッセージをドメインモデルに変換
            var rules = request.Rules.Select(r => new PortForwardRuleModel(
                InterfaceName: r.InterfaceName,
                IsTcp: r.IsTcp,
                SourcePort: r.SourcePort,
                DestIp: r.DestIp,
                DestPort: r.DestPort
            )).ToList();

            // ポートフォワード設定を適用
            await _useCase.ApplyPortForwardsAsync(rules, context.CancellationToken);

            return new ApplyPortForwardsResponse
            {
                Success = true,
                Message = $"Successfully applied {rules.Count} port forward rules"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply port forward rules");
            return new ApplyPortForwardsResponse
            {
                Success = false,
                Message = $"Failed to apply port forwards: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// ポートフォワーディング設定を設定ファイルから取得
    /// </summary>
    /// <param name="request">リクエストメッセージ（空）</param>
    /// <param name="context">gRPCのサーバーコールコンテキスト</param>
    /// <returns>取得結果のレスポンス</returns>
    /// <exception cref="RpcException"></exception>
    public override async Task<GetStoredPortForwardsResponse> GetStoredPortForwards(Google.Protobuf.WellKnownTypes.Empty request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Received GetStoredPortForwards request");

            // 設定を取得
            var rules = await _useCase.GetPortForwardRulesAsync(context.CancellationToken);

            // ドメインモデルを gRPC メッセージに変換
            var response = new GetStoredPortForwardsResponse();
            response.Rules.AddRange(rules.Select(r => new PortForwardRule
            {
                InterfaceName = r.InterfaceName,
                IsTcp = r.IsTcp,
                SourcePort = r.SourcePort,
                DestIp = r.DestIp,
                DestPort = r.DestPort
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stored port forwards");
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to get stored port forwards: {ex.Message}"));
        }
    }

    /// <summary>
    /// 物理ネットワークインターフェースの一覧を取得
    /// </summary>
    /// <param name="request">リクエストメッセージ（空）</param>
    /// <param name="context">gRPCのサーバーコールコンテキスト</param>
    /// <returns>取得結果のレスポンス</returns>
    /// <exception cref="RpcException"></exception>
    public override async Task<GetNetworkInterfaceListResponse> GetNetworkInterfaceList(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Received GetNetworkInterfaceList request");

            var interfaces = await _useCase.GetNetworkInterfacesAsync(context.CancellationToken);

            var response = new GetNetworkInterfaceListResponse();
            response.Interfaces.AddRange(interfaces);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network interface list");
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to get network interface list: {ex.Message}"));
        }
    }
}
