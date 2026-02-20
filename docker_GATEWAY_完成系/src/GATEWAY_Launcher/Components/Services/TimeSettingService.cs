using GATEWAY_Launcher.Components.Models;
using GATEWAY_Launcher.Components.Pages;
using MudBlazor;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace GATEWAY_Launcher.Components.Services
{
    public class TimeSettingService
    {
        private readonly IDialogService _dialogService;
        private readonly GATEWAYCore.TimeConfigService.TimeConfigServiceClient _client;

        public TimeSettingService(IDialogService dialogService, GATEWAYCore.TimeConfigService.TimeConfigServiceClient client)
        {
            _dialogService = dialogService;
            _client = client;
        }

        /// <summary>
        /// 現在の日時などをサーバーから取得
        /// </summary>
        public async Task<TimeSettingModel> GetCurrentTimeAsync()
        {
            try
            {
                //サーバーから現在の日時を取得
                var currentTimeProto = await _client.GetSystemTimeAsync(new Empty());
                DateTime currentTime = currentTimeProto.Time.ToDateTime().ToLocalTime();

                //サーバーからタイムゾーン情報を取得
                var timeZoneResponse = await _client.GetTimeZoneAsync(new Empty());
                string currentTimeZone = $"({timeZoneResponse.UtcOffset}) {timeZoneResponse.TimezoneId}";

                // サーバーからタイムゾーン一覧を取得
                var timeZoneListResponse = await _client.GetTimeZoneListAsync(new Empty());
                var timeZoneList = new List<string?>();
                for (int i = 0; i < timeZoneListResponse.TimezoneIds.Count; i++)
                {
                    var id = timeZoneListResponse.TimezoneIds[i];
                    var offset = timeZoneListResponse.UtcOffsets[i];
                    timeZoneList.Add($"({offset}) {id}");
                }

                // NTP同期設定をサーバーから取得
                NtpSettingModel ntpSettingModel;
                try
                {
                    var ntpConfigResponse = await _client.GetNTPConfigAsync(new Empty());
                    ntpSettingModel = new NtpSettingModel
                    {
                        SyncCheck = ntpConfigResponse.IsNTPSynced,
                        NtpServer = ntpConfigResponse.Server
                    };
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal || ex.StatusCode == StatusCode.Unknown)
                {
                    Console.WriteLine($"NTP設定の取得に失敗しました: {ex.Status.Detail}");
                    throw;
                }

                return new TimeSettingModel
                {
                    CurrentTime = currentTime,
                    TimeZones = timeZoneList,
                    SelectedTimeZone = currentTimeZone,
                    NtpSetting = ntpSettingModel
                };
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"時刻設定の取得中にエラーが発生しました: StatusCode={ex.StatusCode}, Detail={ex.Status.Detail}");
                throw new Exception($"サーバーとの通信に失敗しました: {ex.Status.Detail}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"予期しないエラーが発生しました: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 日時設定変更ダイアログの表示
        /// </summary>
        public async Task<DateTime?> ShowChangeTimeDialogAsync(DateTime CurrentTime)
        {
            var parameters = new DialogParameters
            {
                ["CurrentTime"] = CurrentTime
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialogInstance = await _dialogService.ShowAsync<ChangeTimeDialog>("時間変更", parameters, options);
            var result = await dialogInstance.Result;

            // ダイアログが閉じられたらレスポンスを返す
            if (result != null && !result.Canceled)
            {
                return (DateTime?)result.Data;
            }

            return null;
        }

        /// <summary>
        /// NTP設定変更ダイアログの表示
        /// </summary>
        public async Task<NtpSettingModel?> ShowChangeNtpDialogAsync(NtpSettingModel Model)
        {
            var parameters = new DialogParameters
            {
                ["Model"] = Model
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialogInstance = await _dialogService.ShowAsync<NtpSettingDialog>("NTP設定変更", parameters, options);
            var result = await dialogInstance.Result;

            // ダイアログが閉じられたらレスポンスを返す
            if (result != null && !result.Canceled)
            {
                return (NtpSettingModel?)result.Data;
            }
            return null;
        }

        /// <summary>
        /// 年リストを生成
        /// </summary>
        public List<string> GenerateYearList(DateTime currentDate, int rangeAfter = 30)
        {
            return Enumerable.Range(currentDate.Year, rangeAfter).Select(y => $"{y}年").ToList();
        }

        /// <summary>
        /// 月リストを生成
        /// </summary>
        public List<string> GenerateMonthList()
        {
            return Enumerable.Range(1, 12).Select(m => $"{m}月").ToList();
        }

        /// <summary>
        /// 日リストを生成
        /// </summary>
        public List<string> GenerateDayList(int year, int month)
        {
            // 指定された年と月の日数を取得
            int daysInMonth = DateTime.DaysInMonth(year, month);
            return Enumerable.Range(1, daysInMonth).Select(d => $"{d}日").ToList();
        }

        /// <summary>
        /// 日リストを更新し、選択日を調整する
        /// </summary>
        public List<string> UpdateDayList(EditDateModel editModel)
        {
            // 年と月を編集モデルから取得
            int selectedYear = int.Parse(editModel.Year.Replace("年", ""));
            int selectedMonth = int.Parse(editModel.Month.Replace("月", ""));

            // 日リストを生成
            var days = GenerateDayList(selectedYear, selectedMonth);

            // 現在選択されている日を調整
            int currentDay = int.Parse(editModel.Day.Replace("日", ""));
            int maxDaysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            if (currentDay > maxDaysInMonth)
            {
                editModel.Day = $"{maxDaysInMonth}日";
            }

            return days;
        }

        /// <summary>
        /// 時刻リストを生成 (0〜23時)
        /// </summary>
        public List<string> GenerateTimeList()
        {
            return Enumerable.Range(0, 24).Select(h => $"{h:D2}時").ToList();
        }

        /// <summary>
        /// 分リストを生成 (0〜59分)
        /// </summary>
        public List<string> GenerateMinuteList()
        {
            return Enumerable.Range(0, 60).Select(m => $"{m:D2}分").ToList();
        }

        /// <summary>
        /// サーバーに日時を送信し、変更後の日時を取得
        /// </summary>
        public async Task<DateTime> SendUpdatedDateTimeToServerAsync(Timestamp updatedTimestamp)
        {
            try
            {
                var request = new GATEWAYCore.SystemTimeRequest
                {
                    Time = updatedTimestamp
                };

                var response = await _client.ApplySystemTimeAsync(request);

                return response.Time.ToDateTime().ToLocalTime();
            }
            catch (Exception ex)
            {
                throw new Exception($"サーバーへのリクエスト中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 編集モデルからGoogle.Protobuf.Timestampを生成
        /// </summary>
        public Timestamp ConvertEditDateModelToDateTime(EditDateModel model)
        {
            try
            {
                int year = int.Parse(model.Year.TrimEnd('年'));
                int month = int.Parse(model.Month.TrimEnd('月'));
                int day = int.Parse(model.Day.TrimEnd('日'));
                int hour = int.Parse(model.Time.TrimEnd('時'));
                int minute = int.Parse(model.Minute.TrimEnd('分'));

                DateTime dateTime = new DateTime(year, month, day, hour, minute, 0);

                // UTCに変換してから、Timestampに変換
                return Timestamp.FromDateTime(dateTime.ToUniversalTime());
            }
            catch (Exception ex)
            {
                throw new Exception($"Timestamp変換エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーにタイムゾーン変更を要求
        /// </summary>
        public async Task<string?> SetTimeZoneAsync(string? selectedTimeZone)
        {
            if (string.IsNullOrWhiteSpace(selectedTimeZone))
                throw new ArgumentNullException(nameof(selectedTimeZone), "タイムゾーンが設定されていません");

            try
            {
                // 表示名からタイムゾーンIDを抽出 (例: "(UTC+09:00) Asia/Tokyo" -> "Asia/Tokyo")
                var timeZoneId = ExtractTimeZoneId(selectedTimeZone);

                var request = new GATEWAYCore.TimeZoneRequest
                {
                    Timezone = timeZoneId
                };

                var response = await _client.ApplyTimeZoneAsync(request);
                // レスポンスのIDとオフセットを結合して返す
                return $" ({response.UtcOffset}) {response.TimezoneId}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"タイムゾーン適用中にエラーが発生しました: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 表示名からタイムゾーンIDを抽出
        /// 例: "(UTC+09:00) Asia/Tokyo" → "Asia/Tokyo"
        /// </summary>
        private string ExtractTimeZoneId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return displayName;

            // "(UTC+09:00) " などのパターンを削除
            var index = displayName.IndexOf(") ");
            if (index > 0)
            {
                return displayName.Substring(index + 2).Trim();
            }

            return displayName.Trim();
        }

        /// <summary>
        /// サーバーにホスト名を送信してNTPで時刻設定を実施
        /// </summary>
        public async Task<NtpSettingModel> UpdateTimeUsingNtpAsync(NtpSettingModel model)
        {
            try
            {
                var request = new GATEWAYCore.NTPConfig
                {
                    Server = model.NtpServer,
                    IsNTPSynced = model.SyncCheck
                };

                var response = await _client.ExecSyncNTPAsync(request);
                return new NtpSettingModel
                {
                    NtpServer = response.Server,
                    SyncCheck = response.IsNTPSynced
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NTP同期設定中に失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// サーバーにNTP同期設定を送信し、レスポンスを受け取る
        /// </summary>
        public async Task<NtpSettingModel> UpdateNtpSettingsAsync(NtpSettingModel model)
        {
            try
            {
                var request = new GATEWAYCore.NTPConfig
                {
                    Server = model.NtpServer,
                    IsNTPSynced = model.SyncCheck
                };

                var response = await _client.ApplyNTPConfigAsync(request);

                return new NtpSettingModel
                {
                    NtpServer = response.Server,
                    SyncCheck = response.IsNTPSynced
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NTP同期設定中に失敗: {ex.Message}");
                throw;
            }
        }

        public bool ValidateInput(string NtpServer)
        {
            // 半角英数字とドットのみ許可する正規表現
            var allowedPattern = @"^[a-zA-Z0-9.]+$";

            // 入力値が不正な場合は警告を表示し、処理を中止
            if (string.IsNullOrWhiteSpace(NtpServer) ||
            !System.Text.RegularExpressions.Regex.IsMatch(NtpServer, allowedPattern))
            {
                return false; // 検証失敗
            }

            return true; // 検証成功
        }
    }
}

