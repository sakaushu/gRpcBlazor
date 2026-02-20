using GATEWAY_Launcher.Components.Models;
using GATEWAY_Launcher.Components.Pages;
using GATEWAYCore;
using Google.Protobuf.WellKnownTypes;
using MudBlazor;
using System.Net;
using System.Net.Sockets;
using static GATEWAYCore.NetworkConfigService;
using System.Linq;

namespace GATEWAY_Launcher.Components.Services
{
    public class NicConfigService
    {
        private readonly NicConfigModel _model;
        private readonly NetworkConfigService.NetworkConfigServiceClient _client;
        private readonly IDialogService _dialogService;

        public NicConfigService(NicConfigModel model, NetworkConfigServiceClient client, IDialogService dialogService)
        {
            _model = model;
            _client = client;
            _dialogService = dialogService;
        }

        public NicConfigModel GetModel() => _model;

        /// <summary>
        /// サーバーからNIC設定を取得してモデルに反映
        /// </summary>
        public async Task LoadFromServerAsync()
        {
            var response = await _client.GetAllNicConfigsAsync(new Empty());

            _model.Interfaces = response.Nics.Select(nic => new EthInfo
            {
                Name = nic.InterfaceName,
                Mac = nic.MacAddress,
                DhcpEnabled = nic.Ipv4.Method == Ipv4Method.Dhcp,
                IpAddress = nic.Ipv4.Address,
                SubnetMask = ConvertPrefixToSubnetMask(nic.Ipv4.Prefix)
            }).ToList();

            _model.DefaultGateway = response.Gateway;

            _model.DnsServers = new List<string?>
            {
                response.Dns.Servers.ElementAtOrDefault(0), // Primary DNS
                response.Dns.Servers.ElementAtOrDefault(1)  // Secondary DNS
            };
        }

        /// <summary>
        /// 初期化時にDHCP有効な項目はクリア
        /// </summary>
        public void ClearIpIfDhcpEnabled(NicConfigModel editmodel)
        {
            if (editmodel?.Interfaces != null)
            {
                foreach (var eth in editmodel.Interfaces)
                {
                    if (eth.DhcpEnabled)
                    {
                        eth.IpAddress = string.Empty;
                        eth.SubnetMask = string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// DHCP設定変更時の処理
        /// </summary>
        public void SetDhcpEnabled(EthInfo eth, bool enabled)
        {
            eth.DhcpEnabled = enabled;
            if (enabled)
            {
                eth.IpAddress = string.Empty;
                eth.SubnetMask = string.Empty;
            }
        }

        /// <summary>
        /// サーバーにNIC設定を適用
        /// </summary>
        public async Task ApplySettings(NicConfigModel newModel)
        {
            // ApplyNicConfigsRequestを構築
            var request = new ApplyNicConfigsRequest
            {
                Gateway = newModel.DefaultGateway ?? string.Empty,
                Dns = new DnsConfig()
            };

            // NIC設定を追加
            foreach (var eth in newModel.Interfaces)
            {
                var nicConfig = new NicConfig
                {
                    InterfaceName = eth.Name ?? string.Empty,
                    MacAddress = eth.Mac ?? string.Empty,
                    Ipv4 = new Ipv4Config
                    {
                        Method = eth.DhcpEnabled ? Ipv4Method.Dhcp : Ipv4Method.Static,
                        Address = eth.IpAddress ?? string.Empty,
                        Prefix = ConvertSubnetMaskToPrefix(eth.SubnetMask ?? string.Empty)
                    }
                };
                request.Nics.Add(nicConfig);
            }

            // DNS設定を追加
            foreach (var dns in newModel.DnsServers)
            {
                if (!string.IsNullOrWhiteSpace(dns))
                {
                    request.Dns.Servers.Add(dns);
                }
            }

            try
            {
                // gRPC通信でサーバーに送信
                var response = await _client.ApplyNicConfigsAsync(request);

                // レスポンスをモデルに反映
                _model.Interfaces = response.Nics.Select(nic => new EthInfo
                {
                    Name = nic.InterfaceName,
                    Mac = nic.MacAddress,
                    DhcpEnabled = nic.Ipv4.Method == Ipv4Method.Dhcp,
                    IpAddress = nic.Ipv4.Address,
                    SubnetMask = ConvertPrefixToSubnetMask(nic.Ipv4.Prefix)
                }).ToList();

                _model.DefaultGateway = response.Gateway;

                _model.DnsServers = new List<string?>
                {
                    response.Dns.Servers.ElementAtOrDefault(0), // Primary DNS
                    response.Dns.Servers.ElementAtOrDefault(1)  // Secondary DNS
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の適用に失敗しました: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// サブネットマスクをプレフィックス長に変換
        /// </summary>
        private int ConvertSubnetMaskToPrefix(string subnetMask)
        {
            if (string.IsNullOrWhiteSpace(subnetMask))
            {
                return 0;
            }

            try
            {
                var parts = subnetMask.Split('.');
                if (parts.Length != 4)
                {
                    return 0;
                }

                int prefix = 0;
                foreach (var part in parts)
                {
                    if (int.TryParse(part, out int octet))
                    {
                        prefix += Convert.ToString(octet, 2).Count(c => c == '1');
                    }
                }
                return prefix;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// プレフィックス長をサブネットマスクに変換
        /// </summary>
        private string ConvertPrefixToSubnetMask(int prefix)
        {
            if (prefix < 0 || prefix > 32)
            {
                return "0.0.0.0";
            }

            uint mask = 0xFFFFFFFF << (32 - prefix);
            byte[] bytes = new[]
            {
                (byte)(mask >> 24),
                (byte)(mask >> 16),
                (byte)(mask >> 8),
                (byte)mask
            };

            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        }

        /// <summary>
        /// ネットワーク設定変更ダイアログの表示
        /// </summary>
        public async Task<NicConfigModel?> RequestNicSettingsChangeAsync()
        {

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialogInstance = await _dialogService.ShowAsync<NicSettingsDialog>("ネットワーク設定変更", options);
            var result = await dialogInstance.Result;

            // ダイアログ内でApplySettingsが呼ばれるため、
            // ダイアログが閉じられたら最新のモデルを返す
            if (result != null && !result.Canceled)
            {
                return _model;
            }

            return null;
        }

        /// <summary>
        /// 入力値の検証
        /// </summary>
        public bool ValidateInputs(NicConfigModel EditModel)
        {
            // イーサネット設定の検証
            foreach (var eth in EditModel.Interfaces)
            {
                if (!eth.DhcpEnabled)
                {
                    // DHCP無効の場合は、IPアドレスとサブネットマスクが必須
                    if (string.IsNullOrWhiteSpace(eth.IpAddress) || !IsValidIpAddress(eth.IpAddress))
                    {
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(eth.SubnetMask) || !IsValidSubnetMask(eth.SubnetMask))
                    {
                        return false;
                    }
                }
            }
            // デフォルトゲートウェイの検証 → 必須 & 形式チェック
            if (string.IsNullOrWhiteSpace(EditModel.DefaultGateway) || !IsValidIpAddress(EditModel.DefaultGateway))
            {
                return false;
            }

            // DNS設定の検証 → Primary DNSは必須、Secondary DNSは任意
            if (EditModel.DnsServers == null || EditModel.DnsServers.Count < 1)
            {
                return false;
            }
            // Primary DNS
            if (string.IsNullOrWhiteSpace(EditModel.DnsServers[0]) || !IsValidIpAddress(EditModel.DnsServers[0] ?? ""))
            {
                return false;
            }
            // Secondary DNS (任意)
            if (EditModel.DnsServers.Count >= 2 &&
                !string.IsNullOrWhiteSpace(EditModel.DnsServers[1]) &&
                !IsValidIpAddress(EditModel.DnsServers[1] ?? ""))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// IPアドレス形式の検証
        /// </summary>
        public bool IsValidIpAddress(string ipAddress)
        {
            // パースできなければ false
            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                return false;
            }
            // IPv4 の場合は厳密チェック
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string[] parts = ipAddress.Split('.');
                if (parts.Length != 4)
                {
                    return false;

                }

                foreach (var part in parts)
                {
                    if (!int.TryParse(part, out int value))
                    {
                        return false;
                    }
                    if (value < 0 || value > 255)
                    {
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// サブネットマスク形式の検証
        /// </summary>
        public bool IsValidSubnetMask(string subnetMask)
        {
            if (!IPAddress.TryParse(subnetMask, out IPAddress? ip))
            {
                return false;
            }

            // サブネットマスクとして有効な値かチェック
            byte[] bytes = ip.GetAddressBytes();
            uint mask = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);

            // ビットが連続しているか確認（例: 11111111.11111111.11111111.00000000）
            uint inverted = ~mask;
            return (inverted & (inverted + 1)) == 0;
        }

        /// <summary>
        /// 設定ファイルからネットワーク設定の取得
        /// </summary>
        /// <returns>設定ファイルから取得したネットワーク設定（新しいインスタンス）</returns>
        public async Task<NicConfigModel> GetConfigurationFile()
        {
            var response = await _client.GetStoredNicConfigsAsync(new Empty());

            return new NicConfigModel
            {
                Interfaces = response.Nics.Select(nic => new EthInfo
                {
                    Name = nic.InterfaceName,
                    Mac = nic.MacAddress,
                    DhcpEnabled = nic.Ipv4.Method == Ipv4Method.Dhcp,
                    IpAddress = nic.Ipv4.Address,
                    SubnetMask = ConvertPrefixToSubnetMask(nic.Ipv4.Prefix)
                }).ToList(),
                DefaultGateway = response.Gateway,
                DnsServers = new List<string?>
                {
                    response.Dns.Servers.ElementAtOrDefault(0), // Primary DNS
                    response.Dns.Servers.ElementAtOrDefault(1)  // Secondary DNS
                }
            };
        }

        public bool TryGetDuplicateIp(NicConfigModel modelToCheck, out string duplicateIp)
        {
            duplicateIp = null;

            if (modelToCheck?.Interfaces == null)
                return false;

            var dup = modelToCheck.Interfaces
                .Where(i => !i.DhcpEnabled && !string.IsNullOrWhiteSpace(i.IpAddress))
                .GroupBy(i => i.IpAddress?.Trim())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(dup))
            {
                duplicateIp = dup;
                return true;
            }

            return false;
        }
    }
}