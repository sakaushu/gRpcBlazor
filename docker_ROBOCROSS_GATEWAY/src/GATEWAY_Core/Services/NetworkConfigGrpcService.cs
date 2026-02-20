using GATEWAYCore.Application;
using GATEWAYCore.Application.UseCase;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Infrastructure.Configs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tmds.DBus;

namespace GATEWAYCore.Service;

/// <summary>
/// ネットワーク設定のgRPCサービス実装クラス
/// </summary>
public sealed class NetworkConfigGrpcService : NetworkConfigService.NetworkConfigServiceBase
{
    private readonly NetworkConfigUseCase _networkConfigUseCase;
    private readonly ILogger<NetworkConfigGrpcService> _logger;
    private readonly ConfigsManager _configsManager;

    public NetworkConfigGrpcService(NetworkConfigUseCase networkConfigUseCase, ILogger<NetworkConfigGrpcService> logger, ConfigsManager configsManager)
    {
        _networkConfigUseCase = networkConfigUseCase ?? throw new ArgumentNullException(nameof(networkConfigUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configsManager = configsManager ?? throw new ArgumentNullException(nameof(configsManager));
    }

    /// <summary>
    /// ネットワーク設定を設定を適用する
    /// </summary>
    /// <param name="request">適用するネットワーク設定</param>
    /// <param name="context">gRPC呼び出しの実行コンテキスト</param>
    /// <returns>適用後のネットワーク設定</returns>
    public override async Task<ApplyNicConfigsResponse> ApplyNicConfigs(ApplyNicConfigsRequest request, ServerCallContext context)
    {
        // リクエストの null チェック
        ArgumentNullException.ThrowIfNull(request);

        var requestId = Guid.NewGuid().ToString().Substring(0, 8);
        _logger.LogInformation("[{RequestId}] === ApplyNicConfigs Request Start ===", requestId);

        // 処理用モデルへの変換
        var domainRequest = ToDomainNicsModels(request);

        // 処理用モデルのログ出力
        _logger.LogInformation("DefaultGateway: {Gateway}", domainRequest.DefaultGateway ?? "null");

        // ネットワーク設定の適用
        var domainResponse = await _networkConfigUseCase.ApplyNetworkConfigAsync(domainRequest, context.CancellationToken);

        // レスポンスの null チェック
        ArgumentNullException.ThrowIfNull(domainResponse, nameof(domainResponse));

        // 処理用モデルの変換
        var gRpcResponse = ToGrpcNicsModels(domainResponse);
        _logger.LogInformation("[{RequestId}] === ApplyNicConfigs Response End ===", requestId);
        return gRpcResponse;
    }

    /// <summary>
    /// ネットワーク設定を取得する
    /// </summary>
    /// <param name="request">リクエスト(空)</param>
    /// <param name="context">gRPC呼び出しの実行コンテキスト</param>
    /// <returns>現在のネットワーク設定</returns>
    public override async Task<GetAllNicConfigsResponse> GetAllNicConfigs(Empty request, ServerCallContext context)
    {
        var requestId = Guid.NewGuid().ToString().Substring(0, 8);
        _logger.LogInformation("[{RequestId}] === GetAllNicConfigs Request Start ===", requestId);

        // ネットワーク設定の取得(現在の実設定)
        var domainResponse = await _networkConfigUseCase.GetNetworkConfigAsync(context.CancellationToken);

        // レスポンスの null チェック
        ArgumentNullException.ThrowIfNull(domainResponse, nameof(domainResponse));

        // 処理用モデルの変換
        return ToGetGrpcNicsModels(domainResponse);
    }

    /// <summary>
    /// 設定ファイルからネットワーク設定を取得する
    /// </summary>
    /// <param name="request">リクエスト(空)</param>
    /// <param name="context">gRPC呼び出しの実行コンテキスト</param>
    /// <returns>設定ファイルに保存されたネットワーク設定</returns>
    public override async Task<GetAllNicConfigsResponse> GetStoredNicConfigs(Empty request, ServerCallContext context)
    {
        var requestId = Guid.NewGuid().ToString().Substring(0, 8);
        _logger.LogInformation("[{RequestId}] === GetStoredNicConfigs Request Start ===", requestId);
        var domainResponse = await _networkConfigUseCase.GetNetworkConfigFromFileAsync(context.CancellationToken);
        return ToGetGrpcNicsModels(domainResponse);
    }

    /// <summary>
    /// 設定適用gRPCリクエストを内部処理用に変換する
    /// </summary>
    /// <param name="gRpcRequest">gRPCリクエスト</param>
    /// <returns>内部処理モデル</returns>
    // NotNull 属性を追加してコンパイラに伝える
    [return: NotNull]
    private static NetworkConfigModel ToDomainNicsModels([NotNull] ApplyNicConfigsRequest gRpcRequest)
    {
        ArgumentNullException.ThrowIfNull(gRpcRequest);

        var servers = gRpcRequest.Dns?.Servers?.ToList() ?? new List<string>();

        return new NetworkConfigModel(
            NicConfigs: (gRpcRequest.Nics ?? Enumerable.Empty<NicConfig>()).Select(r => new Domain.Models.NicConfigModel(
                InterfaceName: r.InterfaceName ?? string.Empty,
                MacAddress: r.MacAddress ?? string.Empty,
                Ipv4: new Ipv4ConfigModel(
                    r.Ipv4 != null ? (Domain.Models.Ipv4MethodModel)r.Ipv4.Method : Domain.Models.Ipv4MethodModel.Dhcp,
                    r.Ipv4?.Address ?? string.Empty,
                    r.Ipv4?.Prefix ?? 0
                ),
                devicePath: new ObjectPath("/"),
                connectionPath: new ObjectPath("/"),
                connectionId: ""
            )).ToList(),
            DefaultGateway: gRpcRequest.Gateway ?? string.Empty,
            Dns: new DnsConfigModel(servers)
        );
    }

    /// <summary>
    /// 内部処理用モデルを設定適用gRPCレスポンスに変換する
    /// </summary>
    /// <param name="domainResponse">内部処理モデル</param>
    /// <returns>gRPCレスポンス</returns>
    [return: NotNull]
    private static ApplyNicConfigsResponse ToGrpcNicsModels([NotNull] NetworkConfigModel domainResponse)
    {
        ArgumentNullException.ThrowIfNull(domainResponse);

        var result = new ApplyNicConfigsResponse();

        if (domainResponse.NicConfigs != null && domainResponse.NicConfigs.Any())
        {
            var nicList = domainResponse.NicConfigs.Select(n => new NicConfig
            {
                InterfaceName = n.InterfaceName ?? string.Empty,
                MacAddress = n.MacAddress ?? string.Empty,
                Ipv4 = new Ipv4Config
                {
                    Method = (Ipv4Method)n.Ipv4.Method,
                    Address = n.Ipv4.Address ?? string.Empty,
                    Prefix = n.Ipv4.Prefix ?? 0,
                }
            }).ToList();

            result.Nics.AddRange(nicList);
        }

        if (result.Dns == null)
        {
            result.Dns = new DnsConfig();
        }

        if (domainResponse.Dns?.Servers != null && domainResponse.Dns.Servers.Any())
        {
            result.Dns.Servers.AddRange(domainResponse.Dns.Servers);
        }

        result.Gateway = domainResponse.DefaultGateway ?? string.Empty;

        return result;
    }

    /// <summary>
    /// 内部処理用モデルを設定取得gRPCレスポンスに変換する
    /// </summary>
    /// <param name="domainResponse">内部処理用モデル</param>
    /// <returns>gRPCレスポンス</returns>
    [return: NotNull]
    private static GetAllNicConfigsResponse ToGetGrpcNicsModels([NotNull] NetworkConfigModel domainResponse)
    {
        ArgumentNullException.ThrowIfNull(domainResponse);

        var result = new GetAllNicConfigsResponse();

        if (domainResponse.NicConfigs != null && domainResponse.NicConfigs.Any())
        {
            var nicList = domainResponse.NicConfigs.Select(n => new NicConfig
            {
                InterfaceName = n.InterfaceName ?? string.Empty,
                MacAddress = n.MacAddress ?? string.Empty,
                Ipv4 = new Ipv4Config
                {
                    Method = (Ipv4Method)n.Ipv4.Method,
                    Address = n.Ipv4.Address ?? string.Empty,
                    Prefix = n.Ipv4.Prefix ?? 0,
                }
            }).ToList();

            if (result.Nics != null)
            {
                result.Nics.AddRange(nicList);
            }
        }

        if (result.Dns == null)
        {
            result.Dns = new DnsConfig();
        }

        if (domainResponse.Dns?.Servers != null && domainResponse.Dns.Servers.Any())
        {
            result.Dns.Servers.AddRange(domainResponse.Dns.Servers);
        }
        else
        {
            result.Dns.Servers.AddRange(new[] { "8.8.8.8" });
        }

        if (!string.IsNullOrEmpty(domainResponse.DefaultGateway))
        {
            result.Gateway = domainResponse.DefaultGateway;
        }
        else
        {
            result.Gateway = "192.168.1.1";
        }

        return result;
    }
}
