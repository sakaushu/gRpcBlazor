using GATEWAY_Launcher.Components.Models;

namespace GATEWAY_Launcher.Components.Services
{
    public class HardwareInfoService : IDisposable
    {
        private readonly HardwareInfoModel _model;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private bool _disposed;

        public HardwareInfoService(HardwareInfoModel model)
        {
            _model = model;
        }

        // UI 側に変更を通知するイベント
        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
        // 初回のみ CpuInfo を設定するフラグ
        private bool _cpuInfoInitialized;

        /// <summary>
        /// 指定した間隔でサーバーからハードウェア情報を取得してモデルに反映する処理
        /// </summary>
        /// <param name="intervalMs">更新間隔（ミリ秒）</param>
        public void StartAutoRefresh(int intervalMs = 10000)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HardwareInfoService));
            }

            StopPeriodicLoad();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _pollingTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await LoadFromServerAsync();
                        NotifyStateChanged(); // データ更新後にUIに通知
                        await Task.Delay(intervalMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        // キャンセル時は正常終了
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセル時は正常終了
                        break;
                    }
                    catch (Exception ex)
                    {
                        // ログ出力などのエラーハンドリングを追加
                        Console.WriteLine($"ハードウェア情報の取得中にエラーが発生しました: {ex.Message}");

                        // エラーが発生してもポーリングを継続
                        try
                        {
                            await Task.Delay(intervalMs, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }
            }, token);
        }

        /// <summary>
        /// 定期的なデータ読み込みを停止
        /// </summary>
        public void StopPeriodicLoad()
        {
            _semaphore.Wait();
            try
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                // タスクの完了を待機（タイムアウト付き）
                _pollingTask?.Wait(TimeSpan.FromSeconds(2));
                _pollingTask = null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// サーバーからハードウェア情報を取得してモデルに反映
        /// </summary>
        public async Task LoadFromServerAsync()
        {
            // サーバーからハードウェア情報を取得する処理を実装
            await Task.Delay(100); // ダミーの非同期処理

            // 初回のみ CpuInfo を設定
            if (!_cpuInfoInitialized)
            {
                _model.CpuInfo = "Intel Core i7-9700K";
                _cpuInfoInitialized = true;
            }
            var rand = new Random();
            _model.CpuUsage = rand.NextDouble() * 100;
            _model.CpuTemperature = rand.NextDouble() * 30;
            _model.MemoryUsage = rand.NextDouble() * 30;
            _model.MemoryAvailable = rand.NextDouble() * 30;
            _model.DiskUsage = rand.NextDouble() * 300;
            _model.DiskAvailable = rand.NextDouble() * 500;
        }

        public HardwareInfoModel GetModel() => _model;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopPeriodicLoad();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}