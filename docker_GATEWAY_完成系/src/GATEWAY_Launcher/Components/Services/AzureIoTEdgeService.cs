using GATEWAY_Launcher.Components.Models;
using MudBlazor;
using GATEWAY_Launcher.Components.Pages;
using Google.Protobuf.WellKnownTypes;

namespace GATEWAY_Launcher.Components.Services
{
    public class AzureIoTEdgeService
    {
        private readonly AzureIoTEdgeModel _model;
        private readonly IDialogService _dialogService;
        private readonly ConnectionSettingsModel _connectionSettingsModel;
        private readonly GATEWAYCore.AzIotEdgeConfigService.AzIotEdgeConfigServiceClient _azIotEdgeConfigServiceClient;

        public AzureIoTEdgeService(AzureIoTEdgeModel model, IDialogService dialogService, ConnectionSettingsModel connectionSettingsModel, GATEWAYCore.AzIotEdgeConfigService.AzIotEdgeConfigServiceClient azIotEdgeConfigServiceClient)
        {
            _model = model;
            _dialogService = dialogService;
            _connectionSettingsModel = connectionSettingsModel;
            _azIotEdgeConfigServiceClient = azIotEdgeConfigServiceClient;
        }

        /// <summary>
        /// サーバーからステータスとモジュール情報を取得してモデルに反映
        /// </summary>
        public async Task LoadFromServerAsync()
        {
            // サーバーからAzure IoT Edgeのステータスを取得
            var resStatusMap = await _azIotEdgeConfigServiceClient.GetAzIotEdgeStatusAsync(new Empty());
            var resModuleInfo = await _azIotEdgeConfigServiceClient.GetAzIotEdgeModuleInfoAsync(new Empty());

            _model.StatusMap = new Dictionary<string, string>();
            if (resStatusMap.AziotedgeStatus != null)
            {
                foreach (var kvp in resStatusMap.AziotedgeStatus)
                {
                    _model.StatusMap[kvp.Key] = kvp.Value;
                }
            }

            if (resModuleInfo.ModuleInfoList != null)
            {
                foreach (var moduleInfo in resModuleInfo.ModuleInfoList)
                {
                    _model.Moduledata.Add(new ModuleInfo
                    {
                        Name = moduleInfo.Name,
                        Status = moduleInfo.Status,
                        Description = moduleInfo.Description,
                        Config = moduleInfo.Config
                    });
                }
            }
        }

        public AzureIoTEdgeModel GetModel() => _model;

        /// <summary>
        /// 設定変更ダイアログを表示
        /// </summary>
        public async Task ShowConnectionSettingsDialogAsync()
        {
            try
            {
                var options = new DialogOptions
                {
                    CloseOnEscapeKey = true,
                    MaxWidth = MaxWidth.Small,
                    FullWidth = true
                };

                var dialogInstance = await _dialogService.ShowAsync<ConnectionSettingsDialog>("接続設定", options);
                var result = await dialogInstance.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ダイアログ表示エラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// モデルのクローン作成
        /// </summary>
        public ConnectionSettingsModel CloneModel(ConnectionSettingsModel model)
        {
            return new ConnectionSettingsModel
            {
                IsX509 = model.IsX509,
                ConnectionString = model.ConnectionString,
                Host = model.Host,
                DeviceID = model.DeviceID,
                Identity = new X509Identity
                {
                    CertPath = model.Identity?.CertPath ?? string.Empty,
                    KeyPath = model.Identity?.KeyPath ?? string.Empty
                }
            };
        }

        public async Task<ConnectionSettingsModel> GetConnectionSettingsAsync()
        {
            //サーバーから接続設定を取得する
            var responce = await _azIotEdgeConfigServiceClient.GetAzIotEdgeConnectionConfigAsync(new Empty());

            _connectionSettingsModel.IsX509 = responce.IsX509;
            _connectionSettingsModel.ConnectionString = responce.ConnectionString;
            _connectionSettingsModel.Host = responce.HostName;
            _connectionSettingsModel.DeviceID = responce.DeviceId;
            _connectionSettingsModel.Identity = new X509Identity
            {
                CertPath = responce.Identity?.CertPath ?? string.Empty,
                KeyPath = responce.Identity?.KeyPath ?? string.Empty
            };

            return _connectionSettingsModel;
        }

        public ConnectionSettingsModel GetConnectionSettingsModel() => _connectionSettingsModel;

        public async Task UpdateConnectionSettingsAsync(ConnectionSettingsModel EditModel)
        {
            var request = new GATEWAYCore.AzIotEdgeConnectionConfig
            {
                IsX509 = EditModel.IsX509,
                ConnectionString = EditModel.ConnectionString ?? string.Empty,
                HostName = EditModel.Host ?? string.Empty,
                DeviceId = EditModel.DeviceID ?? string.Empty,
                Identity = new GATEWAYCore.X509Identity
                {
                    CertPath = EditModel.Identity?.CertPath ?? string.Empty,
                    KeyPath = EditModel.Identity?.KeyPath ?? string.Empty
                }
            };

            var response = await _azIotEdgeConfigServiceClient.ApplyAzIotEdgeConnectionConfigAsync(request);
        }
    }
}