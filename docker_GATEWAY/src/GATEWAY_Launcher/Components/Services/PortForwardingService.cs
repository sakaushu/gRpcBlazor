using GATEWAY_Launcher.Components.Models;
using MudBlazor;
using GATEWAY_Launcher.Components.Pages;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Contracts;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GATEWAYCore;
using static GATEWAYCore.ConfigsService;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;

namespace GATEWAY_Launcher.Components.Services
{
    public class PortForwardingService
    {
        private readonly PortForwardingModel _model;
        private readonly IDialogService _dialogService;
        private readonly ISnackbar _snackbar;
        private readonly ConfigsServiceClient _configs;
        private readonly NicConfigService _nicConfigService;
        private readonly GATEWAYCore.PortForwardConfigService.PortForwardConfigServiceClient _portForwardingServiceClient;

        /// <summary>
        /// ポート番号の最小値（NetworkPortRange から取得）
        /// </summary>
        private int _portMinValue = 1;

        /// <summary>
        /// ポート番号の最大値（NetworkPortRange から取得）
        /// </summary>
        private int _portMaxValue = 65535;

        public PortForwardingService(PortForwardingModel model, IDialogService dialogService, ISnackbar snackbar, ConfigsServiceClient configs, NicConfigService nicConfigService, GATEWAYCore.PortForwardConfigService.PortForwardConfigServiceClient portForwardingServiceClient)
        {
            _model = model;
            _dialogService = dialogService;
            _snackbar = snackbar;
            _configs = configs;
            _nicConfigService = nicConfigService;
            _portForwardingServiceClient = portForwardingServiceClient;
        }
        /// <summary>
        /// サーバーからポートフォワーディングの設定を取得し、モデルに反映する
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

            var response = await _portForwardingServiceClient.GetStoredPortForwardsAsync(new Empty());

            _model.Elements = response.Rules.Select(pf => new PortForwardingElement
            {
                Interface = pf.InterfaceName,
                IsTcp = pf.IsTcp,
                ReceptionPort = pf.SourcePort,
                DestinationIp = pf.DestIp,
                DestinationPort = pf.DestPort
            }).ToList();
        }

        public PortForwardingModel GetModel() => _model;

        /// <summary>
        /// ポートフォワーディング設定の追加・編集ダイアログを表示する
        /// </summary> <param name="element">編集する要素（新規作成の場合は null）</param> <param name="model">モデル全体</param>
        public async Task<DialogResult> ShowPortForwardingSettingDialogAsync(PortForwardingElement? element = null, PortForwardingModel? model = null)
        {
            var parameters = new DialogParameters
            {
                ["Element"] = element,
                ["Model"] = model
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialogInstance = await _dialogService.ShowAsync<PortForwardingSettingDialog>("ポートフォワーディング設定", parameters, options);
            var result = await dialogInstance.Result;

            return result;
        }

        /// <summary>
        /// エラーダイアログを表示する
        /// </summary> 
        public async Task ShowErrorDialogAsync(string message)
        {
            await _dialogService.ShowMessageBox("エラー", message, yesText: "OK");
        }

        /// <summary>
        /// 確認ダイアログを表示する
        /// </summary> 
        public async Task<bool> ShowConfirmDialogAsync(string message)
        {
            var result = await _dialogService.ShowMessageBox("確認", message, yesText: "OK", cancelText: "キャンセル");
            return result == true;
        }

        /// <summary>
        /// 重複チェックを行う
        /// </summary> <param name="editElement">編集対象の要素</param> <param name="originalElement">編集前の要素（新規作成の場合は null）</param> <param name="model">モデル全体</param>
        public bool DuplicateCheck(PortForwardingElement editElement, PortForwardingElement? originalElement, PortForwardingModel model)
        {
            //originalElement が null の場合は新規作成として、すべての既存要素と比較し、そうでない場合は編集前の要素を除外して比較
            return model.Elements.Any(e =>
                !(originalElement != null && ReferenceEquals(e, originalElement)) && // 編集前の要素は除外
                e.Interface == editElement.Interface &&
                e.IsTcp == editElement.IsTcp &&
                e.ReceptionPort == editElement.ReceptionPort);
        }

        /// <summary>
        /// 入力値の検証を行う
        /// </summary> <param name="editElement">編集対象の要素</param> <param name="originalElement">編集前の要素（新規作成の場合は null）</param> <param name="model">モデル全体</param>
        public (bool IsValid, string? ErrorMessage) ValidateInputs(PortForwardingElement editElement, PortForwardingElement? originalElement, PortForwardingModel model)
        {
            if (string.IsNullOrWhiteSpace(editElement.Interface))
            {
                return (false, "受信インターフェイスを選択してください。");
            }

            if (string.IsNullOrWhiteSpace(editElement.DestinationIp))
            {
                return (false, "転送先IPアドレスを入力してください。");
            }

            if (editElement.ReceptionPort == null)
            {
                return (false, "受信ポートを入力してください。");
            }

            if (editElement.DestinationPort == null)
            {
                return (false, "転送先ポートを入力してください。");
            }

            if (DuplicateCheck(editElement, originalElement, model))
            {
                return (false, "同じインターフェース、プロトコル、受信ポートの設定が既に存在します。");
            }

            if (!_nicConfigService.IsValidIpAddress(editElement.DestinationIp))
            {
                return (false, "転送先IPアドレスが正しくありません。");
            }

            // 受信ポートの範囲チェック（min/max を使用）
            if (editElement.ReceptionPort < _portMinValue || editElement.ReceptionPort > _portMaxValue)
            {
                return (false, $"受信ポート番号は {_portMinValue} ～ {_portMaxValue} の範囲で入力してください");
            }

            if (editElement.DestinationPort < _portMinValue || editElement.DestinationPort > _portMaxValue)
            {
                return (false, $"転送先ポート番号は {_portMinValue} ～ {_portMaxValue} の範囲で入力してください。");
            }

            return (true, null);
        }

        /// <summary>
        /// サーバーに設定を適用する
        /// </summary> <param name="model">モデル全体</param>
        public async Task<ApplyPortForwardsResponse> ApplySettingsAsync(PortForwardingModel model)
        {
            var request = new ApplyPortForwardsRequest();
            foreach (var element in model.Elements)
            {
                request.Rules.Add(new PortForwardRule
                {
                    InterfaceName = element.Interface ?? string.Empty,
                    IsTcp = element.IsTcp,
                    SourcePort = element.ReceptionPort ?? 0,
                    DestIp = element.DestinationIp ?? string.Empty,
                    DestPort = element.DestinationPort ?? 0
                });
            }

            var response = await _portForwardingServiceClient.ApplyPortForwardsAsync(request);
            return response;
        }

        /// <summary>
        /// サーバーから物理インターフェース一覧を取得
        /// </summary> 
        public async Task<List<string>> GetNetworkInterfacesAsync()
        {
            var response = await _portForwardingServiceClient.GetNetworkInterfaceListAsync(new Empty());
            return response.Interfaces?.ToList() ?? new List<string>();
        }
    }
}
