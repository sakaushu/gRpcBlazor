using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Application.UseCase;
using GATEWAYCore.Infrastructure.Configs;

namespace GATEWAYCore.Infrastructure.HostedServices
{
    /// <summary>
    /// ゲートウェイアプリケーションの起動時の初期化処理を提供します。
    /// </summary>
    public class GatewayStartupService : IHostedLifecycleService
    {
        private readonly ConfigsManager _configsManager;
        private readonly ILogger<GatewayStartupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GatewayStartupService(
            IServiceProvider serviceProvider,
            ConfigsManager configsManager,
            ILogger<GatewayStartupService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configsManager = configsManager ?? throw new ArgumentNullException(nameof(configsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// app.run()が実行され、Kestrelサーバーが起動した後に呼び出されます。
        /// 起動直後に実行したい処理がある場合は、ここに記述してください。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartedAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("ゲートウェイ起動時処理を開始します");

                using var scope = _serviceProvider.CreateScope();
                var httpProxyConfigUseCase = scope.ServiceProvider.GetRequiredService<HTTPProxyConfigUseCase>();

                // HTTPプロキシ設定をマシンに適用
                await httpProxyConfigUseCase.ApllyConfigToMachine();

                _logger.LogInformation("ゲートウェイ起動時処理が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ゲートウェイ起動時処理でエラーが発生しました");
                throw;
            }
        }

        public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

