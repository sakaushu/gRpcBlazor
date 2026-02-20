using GATEWAYCore;
using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Application.UseCase;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Infrastructure.Configs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GATEWAYCore.Service
{
    /// <summary>
    /// HTTP および HTTPS プロキシ設定を管理するための gRPC サービスメソッドを提供します。
    /// 新しいプロキシ設定の適用と現在の設定の取得が含まれます。
    /// </summary>
    /// <remarks>このサービスは、gRPC 経由でサーバー上のプロキシ設定をプログラムで構成または
    /// 照会する必要があるクライアントが使用することを目的としています。すべての操作は非同期で実行され、結果は
    /// 対応するレスポンスメッセージで返されます。各リクエストに対してログが記録され、監視と
    /// 診断に役立ちます。スレッドセーフティと同時実行性は、基盤となる gRPC フレームワークによって管理されます。</remarks>
    public class HTTPProxyConfigGrpcService : HTTPProxyConfigService.HTTPProxyConfigServiceBase
    {
        private readonly HTTPProxyConfigUseCase _httpProxyConfigUseCase;
        private readonly ConfigsManager _configsManager;
        private readonly ILogger<HTTPProxyConfigGrpcService> _logger;

        public HTTPProxyConfigGrpcService(HTTPProxyConfigUseCase httpProxyConfigUseCase, ConfigsManager configsManager, ILogger<HTTPProxyConfigGrpcService> logger)
        {
            _httpProxyConfigUseCase = httpProxyConfigUseCase ?? throw new ArgumentNullException(nameof(httpProxyConfigUseCase));
            _configsManager = configsManager ?? throw new ArgumentNullException(nameof(configsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// HTTP および HTTPS プロキシ設定を適用します。
        /// リクエストを受け取り、プロキシ設定情報をログに記録した後、設定結果を返します。
        /// </summary>
        /// <param name="request">HTTP および HTTPS プロキシ設定を含むリクエスト。null は許可されません。</param>
        /// <param name="context">サーバー側呼び出しのコンテキスト。デッドライン、キャンセル、メタデータなどの情報を提供します。</param>
        /// <returns>非同期操作を表すタスク。タスク結果には、設定の成功状態と適用されたプロキシ設定を含む <see cref="SetHTTPProxyResponse"/> が含まれます。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> が null の場合にスローされます。</exception>
        public override async Task<SetHTTPProxyResponse> SetHTTPProxy(SetHTTPProxyRequest request, ServerCallContext context)
        {
            try
            {
                // リクエストの null チェック
                ArgumentNullException.ThrowIfNull(request);

                var requestId = Guid.NewGuid().ToString().Substring(0, 8);
                _logger.LogInformation("[{RequestId}] === ApplyNicConfigs Request Start ===", requestId);

                // gRPCリクエストのログ出力
                if (request.HttpProxy != null)
                {
                    _logger.LogInformation("HTTP Proxy: {HttpProxy}", request.HttpProxy);
                }
                else
                {
                    _logger.LogInformation("HTTP Proxy: null");
                }

                if (request.HttpsProxy != null)
                {
                    _logger.LogInformation("HTTPS Proxy: {HttpsProxy}", request.HttpsProxy);
                }
                else
                {
                    _logger.LogInformation("HTTPS Proxy: null");
                }

                await _httpProxyConfigUseCase.SetHTTPProxyConfigAsync(new ProxyConfig(
                    HTTPProxyConfig.FromHTTPProxySetting(request.HttpProxy),
                    HTTPSProxyConfig.FromHTTPSProxySetting(request.HttpsProxy)), context.CancellationToken);

                _logger.LogInformation("[{RequestId}] === ApplyNicConfigs Request End ===", requestId);

                return new SetHTTPProxyResponse
                {
                    Success = true,
                    HttpProxy = request.HttpProxy,
                    HttpsProxy = request.HttpsProxy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting HTTP/HTTPS proxy settings.");

                return new SetHTTPProxyResponse
                {
                    Success = false,
                    HttpProxy = request.HttpProxy,
                    HttpsProxy = request.HttpsProxy
                };
            }
        }

        /// <summary>
        /// サーバーに設定されている現在の HTTP および HTTPS プロキシ設定を取得します。
        /// ※現在は固定のダミーデータを返します。
        /// </summary>
        /// <param name="request">空のリクエストメッセージ。このパラメータはサービス定義で必要ですが、使用されません。</param>
        /// <param name="context">サーバー側呼び出しのコンテキスト。デッドライン、キャンセル、メタデータなどの情報を提供します。</param>
        /// <returns>非同期操作を表すタスク。タスク結果には、現在の HTTP および HTTPS プロキシ設定を含む <see cref="GetHTTPProxyResponse"/> が含まれます。</returns>
        public override async Task<GetHTTPProxyResponse> GetHTTPProxy(Empty request, ServerCallContext context)
        {
            var proxyConfig = await _httpProxyConfigUseCase.GetHTTPProxyConfigAsync(context.CancellationToken);

            return new GetHTTPProxyResponse
            {
                HttpProxy = new HTTPProxySetting
                {
                    Enabled = proxyConfig.HttpProxy.IsEnabled,
                    ProxyAddress = proxyConfig.HttpProxy.ProxyAddress,
                    ProxyPort = proxyConfig.HttpProxy.ProxyPort,
                    Username = proxyConfig.HttpProxy.Username,
                    Password = proxyConfig.HttpProxy.Password
                },
                HttpsProxy = new HTTPSProxySetting
                {
                    Enabled = proxyConfig.HttpsProxy.IsEnabled,
                    ProxyAddress = proxyConfig.HttpsProxy.ProxyAddress,
                    ProxyPort = proxyConfig.HttpsProxy.ProxyPort,
                    Username = proxyConfig.HttpsProxy.Username,
                    Password = proxyConfig.HttpsProxy.Password
                }
            };
        }

    }
}
