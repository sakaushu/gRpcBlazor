using GATEWAYCore;
using Google.Protobuf.WellKnownTypes;
using System.Globalization;

namespace GATEWAY_Launcher.Components.Services
{
    /// <summary>
    /// ConfigsServiceClient の拡張メソッド
    /// </summary>
    public static class ConfigsServiceClientEx
    {
        /// <summary>
        /// 設定値を指定した型で取得する
        /// </summary>
        public static async Task<T> GetConfigAsync<T>(
            this ConfigsService.ConfigsServiceClient client,
            ConfigKey key) where T : notnull
        {
            var request = new GetConfigRequest { Key = (int)key };
            var response = await client.GetConfigAsync(request);
            
            // 型に応じて変換
            if (typeof(T) == typeof(string))
            {
                return (T)(object)response.Value;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(response.Value);
            }
            else if (typeof(T) == typeof(uint))
            {
                // "0x" プレフィックス付きの16進数文字列をパース
                string hexValue = response.Value;
                if (hexValue != "")
                {
                    if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)uint.Parse(hexValue.Substring(2), NumberStyles.HexNumber);
                    }
                    return (T)(object)uint.Parse(hexValue, NumberStyles.HexNumber);
                }
                return (T)(object)0u;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Parse(response.Value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(response.Value);
            }
            else if (typeof(T) == typeof(bool))
            {
                return (T)(object)bool.Parse(response.Value);
            }
            
            throw new NotSupportedException($"型 {typeof(T).Name} はサポートされていません");
        }

        /// <summary>
        /// 設定値を指定した型で設定する
        /// 型に応じて適切に文字列変換（Hex/String/Decimal対応）
        /// </summary>
        public static async Task<SetConfigResponse> SetConfigAsync<T>(
            this ConfigsService.ConfigsServiceClient client,
            ConfigKey key,
            T value) where T : notnull
        {
            var stringValue = ConvertValueToString(value);
            var request = new SetConfigRequest { Key = (int)key, Value = stringValue };
            return await client.SetConfigAsync(request);
        }

        /// <summary>
        /// 設定をファイルに保存する（Empty を自動生成）
        /// </summary>
        public static async Task<SaveConfigResponse> SaveConfigAsync(
            this ConfigsService.ConfigsServiceClient client)
        {
            return await client.SaveConfigAsync(new Empty());
        }

        /// <summary>
        /// 設定値の範囲情報を指定した型で取得する
        /// 数値型（int, uint, long）のみサポート。String は not supported
        /// </summary>
        public static async Task<(T Min, T Max, T Value)> GetConfigRangeAsync<T>(
            this ConfigsService.ConfigsServiceClient client,
            ConfigKey key) where T : struct
        {
            var request = new GetConfigRequest { Key = (int)key };
            var response = await client.GetConfigRangeAsync(request);

            if (!response.Found)
            {
                throw new InvalidOperationException($"設定が見つかりません: {key}");
            }

            // 値を型に応じて変換
            var min = ConvertToType<T>(response.MinValue.ToString());
            var max = ConvertToType<T>(response.MaxValue.ToString());
            var value = ConvertToType<T>(response.Value);

            return (min, max, value);
        }

        /// <summary>
        /// 値を型に応じた文字列に変換します（Hex/String/Decimal対応）
        /// </summary>
        private static string ConvertValueToString<T>(T value) where T : notnull
        {
            if (typeof(T) == typeof(string))
            {
                return (string)(object)value;
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(double))
            {
                // 10進数
                return value.ToString() ?? "";
            }
            else if (typeof(T) == typeof(uint))
            {
                // Hex 値（"0x" プレフィックス付き）
                uint uintVal = (uint)(object)value;
                return $"0x{uintVal:X8}";
            }
            else if (typeof(T) == typeof(bool))
            {
                // bool は "true" または "false"
                return ((bool)(object)value).ToString().ToLower();
            }
            else
            {
                throw new NotSupportedException($"型 {typeof(T).Name} はサポートされていません");
            }
        }

        /// <summary>
        /// 文字列値を指定された型に変換します
        /// </summary>
        private static T ConvertToType<T>(string value) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            else if (typeof(T) == typeof(uint))
            {
                // Hex 値をパース
                uint uintVal;
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    uintVal = uint.Parse(value.Substring(2), NumberStyles.HexNumber);
                }
                else
                {
                    uintVal = uint.Parse(value, NumberStyles.HexNumber);
                }
                return (T)(object)uintVal;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Parse(value);
            }
            else
            {
                throw new NotSupportedException($"型 {typeof(T).Name} はサポートされていません。int, uint, long のみサポート");
            }
        }
    }
}
