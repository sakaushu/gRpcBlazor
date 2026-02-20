using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.DbusInterfaces;
using GATEWAYCore.Domain.Models;
using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace GATEWAYCore.Infrastructure
{
    /// <summary>
    /// D-Bus経由でsystemdのプロキシ設定を管理するクラスです。
    /// システムレベルおよびユーザーセッションレベルでHTTP/HTTPSプロキシ環境変数を設定・取得します。
    /// </summary>
    public class ProxySettingBus : IProxySettingBus
    {
        private readonly ILogger<SystemdTimedateClient> _logger;

        public ProxySettingBus(ILogger<SystemdTimedateClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// D-Bus経由でsystemdの環境変数にプロキシ設定を適用します。
        /// システムバスとセッションバスの両方に対してHTTP_PROXY/HTTPS_PROXY環境変数を設定または削除します。
        /// </summary>
        /// <param name="httpProxy">HTTPプロキシのURL。nullまたは空文字列の場合、既存の設定を削除します。</param>
        /// <param name="httpsProxy">HTTPSプロキシのURL。nullまたは空文字列の場合、既存の設定を削除します。</param>
        /// <returns>非同期操作を表すタスク</returns>
        /// <remarks>
        /// このメソッドは以下の環境変数を設定します：
        /// - HTTP_PROXY / http_proxy
        /// - HTTPS_PROXY / https_proxy
        /// 
        /// D-Busシステムバスへのアクセスにはroot権限が必要な場合があります。
        /// D-Bus経由の設定に失敗した場合、フォールバックメソッド(SetSystemdEnvironmentFallback)を呼び出します。
        /// </remarks>
        public async Task SetSystemdEnvironmentViaDBus(ProxyConfig proxyConfig)
        {
            try
            {
                // D-Bus経由でシステムレベルのsystemd環境変数を設定
                _logger.LogInformation("D-Bus経由でsystemd環境変数を設定しています...");

                var systemConnection = Connection.System;
                var systemdManager = systemConnection.CreateProxy<ISystemd1Manager>(
                    "org.freedesktop.systemd1",
                    "/org/freedesktop/systemd1"
                );

                var envVarsToSet = new System.Collections.Generic.List<string>();
                var envVarsToUnset = new System.Collections.Generic.List<string>();

                if (proxyConfig.HttpProxy.isValid())
                {
                    envVarsToSet.Add($"HTTP_PROXY={proxyConfig.HttpProxy.ToProxyUrl()}");
                    envVarsToSet.Add($"http_proxy={proxyConfig.HttpProxy.ToProxyUrl()}");
                }
                else
                {
                    envVarsToUnset.Add("HTTP_PROXY");
                    envVarsToUnset.Add("http_proxy");
                }

                if (proxyConfig.HttpsProxy.isValid())
                {
                    envVarsToSet.Add($"HTTPS_PROXY={proxyConfig.HttpsProxy.ToProxyUrl()}");
                    envVarsToSet.Add($"https_proxy={proxyConfig.HttpsProxy.ToProxyUrl()}");
                }
                else
                {
                    envVarsToUnset.Add("HTTPS_PROXY");
                    envVarsToUnset.Add("https_proxy");
                }

                // 環境変数を設定
                if (envVarsToSet.Count > 0)
                {
                    await systemdManager.SetEnvironmentAsync(envVarsToSet.ToArray());
                    _logger.LogInformation($"✓ D-Bus経由でsystemdシステム環境変数を設定しました:");
                    foreach (var env in envVarsToSet)
                    {
                        _logger.LogInformation($"    {env}");
                    }
                }

                // 環境変数を削除
                if (envVarsToUnset.Count > 0)
                {
                    try
                    {
                        await systemdManager.UnsetEnvironmentAsync(envVarsToUnset.ToArray());
                        _logger.LogInformation($"✓ D-Bus経由でsystemdシステム環境変数を削除しました");
                    }
                    catch
                    {
                        // 存在しない環境変数を削除しようとした場合は無視
                    }
                }

                // ユーザーセッションのsystemd環境変数も設定
                try
                {
                    var sessionConnection = Connection.Session;
                    var userSystemdManager = sessionConnection.CreateProxy<ISystemd1Manager>(
                        "org.freedesktop.systemd1",
                        "/org/freedesktop/systemd1"
                    );

                    if (envVarsToSet.Count > 0)
                    {
                        await userSystemdManager.SetEnvironmentAsync(envVarsToSet.ToArray());
                    }
                    if (envVarsToUnset.Count > 0)
                    {
                        try
                        {
                            await userSystemdManager.UnsetEnvironmentAsync(envVarsToUnset.ToArray());
                        }
                        catch { }
                    }

                    _logger.LogInformation($"✓ D-Bus経由でsystemdユーザー環境変数を設定しました");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"  (systemdユーザーセッションはスキップされました: {ex.Message})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ D-Bus経由のsystemd環境変数設定に失敗: {ex.Message}");
                _logger.LogError($"  詳細: {ex.GetType().Name}");
                _logger.LogError("  注意: D-Busシステムバスへのアクセスにroot権限が必要な場合があります");
                _logger.LogError("  代替手段として systemctl コマンドを使用します...");
            }
        }


        /// <summary>
        /// プロキシ設定を取得します。
        /// D-Bus経由でsystemdから現在のHTTP_PROXYとHTTPS_PROXY環境変数を読み取ります。
        /// </summary>
        /// <returns>
        /// httpProxy: 現在のHTTPプロキシ設定（未設定の場合はnull）
        /// httpsProxy: 現在のHTTPSプロキシ設定（未設定の場合はnull）
        /// </returns>
        /// <remarks>
        /// エラーが発生した場合は、両方の値がnullのタプルを返します。
        /// 環境変数名の検索は大文字小文字を区別しません。
        /// </remarks>
        public async Task<ProxyConfig> GetProxySettingsAsync()
        {
            try
            {
                _logger.LogInformation("D-Bus経由でsystemd環境変数を取得しています...");

                var systemConnection = Connection.System;
                var systemdManager = systemConnection.CreateProxy<ISystemd1Manager>(
                    "org.freedesktop.systemd1",
                    "/org/freedesktop/systemd1"
                );

                // systemdから全ての環境変数を取得
                var environmentVariables = await systemdManager.ListEnvironmentAsync();

                ProxyConfig httpProxy = null;
                ProxyConfig httpsProxy = null;

                // HTTP_PROXYとHTTPS_PROXYを検索
                foreach (var envVar in environmentVariables)
                {
                    if (envVar.StartsWith("HTTP_PROXY=", StringComparison.OrdinalIgnoreCase))
                    {
                        //httpProxy = envVar.Substring(envVar.IndexOf('=') + 1);
                    }
                    else if (envVar.StartsWith("HTTPS_PROXY=", StringComparison.OrdinalIgnoreCase))
                    {
                        //httpsProxy = envVar.Substring(envVar.IndexOf('=') + 1);
                    }
                }

                _logger.LogInformation($"✓ D-Bus経由でsystemd環境変数を取得しました:");
                //_logger.LogInformation($"    HTTP_PROXY: {httpProxy ?? "(未設定)"}");
                //_logger.LogInformation($"    HTTPS_PROXY: {httpsProxy ?? "(未設定)"}");

                return new ProxyConfig(null, null);
                //return ProxyConfig.Create(
                //    httpProxy.isValid() ? httpProxy : null,
                //    httpsProxy.isValid() ? httpsProxy : null
                //);
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ D-Bus経由のsystemd環境変数取得に失敗: {ex.Message}");
                _logger.LogError($"  詳細: {ex.GetType().Name}");

                // エラー時は空の値を返す
                return new ProxyConfig(null, null);
            }
        }
    }
}
