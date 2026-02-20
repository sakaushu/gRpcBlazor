  using GATEWAY_Launcher.Components.Models;
using GATEWAYCore;
using Google.Protobuf.WellKnownTypes;
using MudBlazor;
using System.Text.RegularExpressions;
using static GATEWAYCore.ConfigsService;

namespace GATEWAY_Launcher.Components.Services
{
    public class ProxyService
    {
        private readonly ISnackbar _snackbar;
        private readonly ProxyModel _model;
        private readonly ConfigsServiceClient _configs;
        private readonly GATEWAYCore.HTTPProxyConfigService.HTTPProxyConfigServiceClient _client;
        
        /// <summary>
        /// ポート番号の最小値（NetworkPortRange から取得）
        /// </summary>
        private int _portMinValue = 1;
        
        /// <summary>
        /// ポート番号の最大値（NetworkPortRange から取得）
        /// </summary>
        private int _portMaxValue = 65535;

        public ProxyService(ISnackbar snackbar, ProxyModel model, ConfigsServiceClient configs, GATEWAYCore.HTTPProxyConfigService.HTTPProxyConfigServiceClient client)
        {
            _snackbar = snackbar;
            _model = model;
            _configs = configs;
            _client = client;
        }

        /// <summary>
        /// サーバーから環境変数を取得してモデルに反映
        /// </summary>
        public async Task LoadFromServerAsync()
        {
            try
            {
                // 範囲情報を取得
                var (min, max, rangeValue) = await _configs.GetConfigRangeAsync<int>(ConfigKey.NetworkPortRange);
                
                // フィールドに保持
                _portMinValue = min;
                _portMaxValue = max;
               
            }
            catch (Exception ex)
            {
                _snackbar.Add($"ポート範囲情報の取得エラー: {ex.Message}", Severity.Warning);
            }

            // サーバーからプロキシ設定を取得
            var response = await _client.GetHTTPProxyAsync(new Empty());

            var http = response.HttpProxy;

            _model.SelectedProxyType = http.Enabled;
            _model.ProxyServer = http.ProxyAddress;
            _model.ProxyPort = http.ProxyPort;
            _model.UserName = http.Username;
            _model.Password = http.Password;

            if(!_model.SelectedProxyType)
            {
                // プロキシ未使用の場合、プロキシサーバとポートの値はクリア
                _model.ProxyServer = null;
                _model.ProxyPort = null;
            }
        }

        public ProxyModel GetModel() => _model;

        /// <summary>
        /// 入力値のバリデーション
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateInputs(ProxyModel model)
        {
            if (!model.SelectedProxyType)
            {
                return (true, null);
            }

            // 空欄チェック
            if (string.IsNullOrWhiteSpace(model.ProxyServer) || model.ProxyPort == null)
            {
                return (false, "値を入力してください");
            }

            // プロキシサーバーの形式チェック（半角英数字と.のみ）
            if (!Regex.IsMatch(model.ProxyServer, @"^[a-zA-Z0-9.]+$"))
            {
                return (false, "プロキシサーバーは半角英数字と「.」のみ入力可能です");
            }

            // ポートの形式チェック（半角数字のみ）
            if (!Regex.IsMatch(model.ProxyPort?.ToString() ?? string.Empty, @"^\d+$"))
            {
                return (false, "ポートは半角数字のみ入力可能です");
            }

            // ポートの範囲チェック（min/max を使用）
            if (model.ProxyPort < _portMinValue || model.ProxyPort > _portMaxValue)
            {
                return (false, $"ポート番号は {_portMinValue} ～ {_portMaxValue} の範囲で入力してください");
            }

            return (true, null);
        }

        /// <summary>
        /// プロキシ設定の保存
        /// </summary>
        public async Task<ProxyModel> SaveProxySettings(ProxyModel newModel)
        {
            var request = new GATEWAYCore.SetHTTPProxyRequest
            {
                // HTTPProxySettingを構築
                HttpProxy = new GATEWAYCore.HTTPProxySetting
                {
                    Enabled = newModel.SelectedProxyType,
                    ProxyAddress = newModel.ProxyServer ?? string.Empty,
                    ProxyPort = newModel.ProxyPort ?? 0,
                    Username = newModel.UserName ?? string.Empty,
                    Password = newModel.Password ?? string.Empty
                },
                // HTTPSProxySettingを構築
                HttpsProxy = new GATEWAYCore.HTTPSProxySetting
                {
                    Enabled = newModel.SelectedProxyType,
                    ProxyAddress = newModel.ProxyServer ?? string.Empty,
                    ProxyPort = newModel.ProxyPort ?? 0,
                    Username = newModel.UserName ?? string.Empty,
                    Password = newModel.Password ?? string.Empty
                }
            };
             
            var response = await _client.SetHTTPProxyAsync(request);

            if(response.Success)
            {
                var http = response.HttpProxy;

                return new ProxyModel
                {
                    SelectedProxyType = http.Enabled,
                    ProxyServer = http.ProxyAddress,
                    ProxyPort = http.ProxyPort,
                    UserName = http.Username,
                    Password = http.Password
                };
            }
            else
            {
                throw new Exception("サーバー側でプロキシ設定の保存に失敗しました。");
            }
        }
    }
}