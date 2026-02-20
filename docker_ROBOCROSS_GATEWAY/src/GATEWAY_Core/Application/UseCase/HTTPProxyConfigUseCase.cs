using System.Threading;
using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.Domain.Models;
using GATEWAYCore.Infrastructure.Configs;

namespace GATEWAYCore.Application.UseCase
{
    /// <summary>
    /// プロキシ設定バスを使用して、HTTPプロキシ構成を取得および更新するためのメソッドを提供します。
    /// </summary>
    /// <remarks> このクラスは、HTTPプロキシ構成操作へのスレッドセーフなアクセスを保証します。すべてのメソッドは非同期であり、呼び出し元のスレッドをブロックしないように待機する必要があります。
    /// プロキシ構成操作中に発生したエラー条件については、ログに記録されます。</remarks>
    public class HTTPProxyConfigUseCase
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly IProxySettingBus _proxySettingBus;
        private readonly ConfigsManager _configsManager;
        private readonly ILogger<HTTPProxyConfigUseCase> _logger;

        public HTTPProxyConfigUseCase(IProxySettingBus proxySettingBus, ConfigsManager configsManager, ILogger<HTTPProxyConfigUseCase> logger)
        {
            _proxySettingBus = proxySettingBus ?? throw new ArgumentNullException(nameof(proxySettingBus));
            _configsManager = configsManager ?? throw new ArgumentNullException(nameof(configsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// configからプロキシ構成を取得し、プロキシ設定バスを使用してマシンに適用します。
        /// </summary>
        /// <returns></returns>
        public async Task ApllyConfigToMachine()
        {
            // configから値を取得
            var proxyEnable = _configsManager.GetConfig(ConfigKey.ProxyEnable) ?? Constants.StrEnabled;
            var proxyAddress = _configsManager.GetConfig(ConfigKey.ProxyAddress) ?? string.Empty;

            // 取得した値をもとにプロキシ構成を作成
            var httpProxyConfig = HTTPProxyConfig.FromConfigFile(proxyEnable, proxyAddress);
            var httpsProxyConfig = HTTPSProxyConfig.FromConfigFile(proxyEnable, proxyAddress);
            var proxyConfig = new ProxyConfig(httpProxyConfig, httpsProxyConfig);

            // プロキシ構成を設定
            await _proxySettingBus.SetSystemdEnvironmentViaDBus(proxyConfig);
        }

        /// <summary>
        /// 指定されたプロキシ構成でHTTPプロキシ構成を設定します。
        /// </summary>
        /// <param name="proxyConfig">設定するプロキシ構成。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>設定されたプロキシ構成。</returns>
        /// <exception cref="ArgumentNullException">proxyConfig または ct が null の場合。</exception>
        /// <exception cref="Exception">プロキシ構成の設定中にエラーが発生した場合。</exception>
        public async Task<ProxyConfig> SetHTTPProxyConfigAsync(ProxyConfig proxyConfig, CancellationToken ct)
        {
            await _lock.WaitAsync(ct);

            try
            {
                await _proxySettingBus.SetSystemdEnvironmentViaDBus(proxyConfig);

                // 設定ファイルへ出力
                _configsManager.SetConfig(ConfigKey.ProxyAddress, proxyConfig.HttpProxy.ToConfigFormatProxyAddress());
                _configsManager.SetConfig(ConfigKey.ProxyEnable, proxyConfig.HttpProxy.IsEnabled ? 1 : 0);
                await _configsManager.SaveConfig();

                return proxyConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTPプロキシ構成の設定中にエラーが発生しました");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 現在のHTTPプロキシ構成を取得します。
        /// </summary>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>現在のプロキシ構成。</returns>
        /// <exception cref="Exception">プロキシ構成の取得中にエラーが発生した場合。</exception>
        public async Task<ProxyConfig> GetHTTPProxyConfigAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct);

            try
            {
                var proxyAddressFromConfig = _configsManager.GetConfig(ConfigKey.ProxyAddress) ?? "";
                var proxyEnableFromConfig = _configsManager.GetConfig(ConfigKey.ProxyEnable) ?? "";

                var httpProxyConfig = HTTPProxyConfig.FromConfigFile(proxyEnableFromConfig, proxyAddressFromConfig);
                var httpsProxyConfig = HTTPSProxyConfig.FromConfigFile(proxyEnableFromConfig, proxyAddressFromConfig);

                return new ProxyConfig(httpProxyConfig, httpsProxyConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTPプロキシ構成の取得中にエラーが発生しました");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
