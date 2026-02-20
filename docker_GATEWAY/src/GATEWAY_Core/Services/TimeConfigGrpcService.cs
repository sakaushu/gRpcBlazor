using GATEWAYCore.Application.UseCase;
using GATEWAYCore;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GATEWAYCore.Domain.Logic;

namespace GATEWAYCore.Service;

/// <summary>
/// 時間設定gRPCサービス実装クラス
/// </summary>
public sealed class TimeConfigGrpcService : TimeConfigService.TimeConfigServiceBase
{
    private readonly TimeConfigUseCase _useCase;
    private readonly ILogger<TimeConfigGrpcService> _logger;

    public TimeConfigGrpcService(TimeConfigUseCase useCase, ILogger<TimeConfigGrpcService> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    public override async Task<SystemTimeResponse> GetSystemTime(Empty request, ServerCallContext context)
    {
        try
        {
            var currentTime = await _useCase.GetSystemTimeAsync(context.CancellationToken);
            return new SystemTimeResponse
            {
                Time = Timestamp.FromDateTime(DateTime.SpecifyKind(currentTime, DateTimeKind.Utc))
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get system time");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get system time"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting system time");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// システム時刻を設定し、設定後の時刻を返す
    /// </summary>
    public override async Task<SystemTimeResponse> ApplySystemTime(SystemTimeRequest request, ServerCallContext context)
    {
        try
        {
            var requestedTime = request.Time.ToDateTime().ToUniversalTime();
            await _useCase.ApplySystemTimeAsync(requestedTime, context.CancellationToken);

            // 設定後の実際の時刻を取得して返す
            var actualTime = await _useCase.GetSystemTimeAsync(context.CancellationToken);
            return new SystemTimeResponse
            {
                Time = Timestamp.FromDateTime(DateTime.SpecifyKind(actualTime, DateTimeKind.Utc))
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied to set system time");
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied to set system time"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to apply system time");
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to set system time: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while applying system time");
            throw new RpcException(new Status(StatusCode.Unknown, $"Failed to set system time: {ex.Message}"));
        }
    }

    public override async Task<TimeZoneResponse> GetTimeZone(Empty request, ServerCallContext context)
    {
        try
        {
            var timezoneId = await _useCase.GetTimezoneAsync(context.CancellationToken);
            var utcOffset = TimeZoneOffsetHelper.GetUtcOffsetString(timezoneId);
            return new TimeZoneResponse
            {
                TimezoneId = timezoneId,
                UtcOffset = utcOffset
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get timezone");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get timezone"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting timezone");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }

    public override async Task<TimeZoneResponse> ApplyTimeZone(TimeZoneRequest request, ServerCallContext context)
    {
        try
        {
            await _useCase.SetTimezoneAsync(request.Timezone, context.CancellationToken);
            var currentTimezoneId = await _useCase.GetTimezoneAsync(context.CancellationToken);
            var utcOffset = TimeZoneOffsetHelper.GetUtcOffsetString(currentTimezoneId);
            return new TimeZoneResponse
            {
                TimezoneId = currentTimezoneId,
                UtcOffset = utcOffset
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (Tmds.DBus.DBusException ex) when (ex.ErrorName == "org.freedesktop.DBus.Error.InvalidArgs")
        {
            _logger.LogError(ex, $"Invalid timezone: {request.Timezone}");
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid timezone '{request.Timezone}'. Use 'Asia/Tokyo' format."));
        }
        catch (Tmds.DBus.DBusException ex) when (ex.ErrorName == "org.freedesktop.DBus.Error.AccessDenied")
        {
            _logger.LogError(ex, "DBus access denied to set timezone");
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied. Please configure polkit or run with appropriate privileges."));
        }
        catch (Tmds.DBus.DBusException ex)
        {
            _logger.LogError(ex, $"DBus error while setting timezone: {ex.ErrorName} - {ex.ErrorMessage}");
            throw new RpcException(new Status(StatusCode.Internal, $"DBus error: {ex.ErrorMessage}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to set timezone");
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to set timezone: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while applying timezone");
            throw new RpcException(new Status(StatusCode.Unknown, $"Failed to set timezone: {ex.Message}"));
        }
    }

    public override async Task<TimeZoneListResponse> GetTimeZoneList(Empty request, ServerCallContext context)
    {
        try
        {
            var timezones = await _useCase.ListTimezonesAsync(context.CancellationToken);
            
            var response = new TimeZoneListResponse();
            response.TimezoneIds.AddRange(timezones);
            response.UtcOffsets.AddRange(timezones.Select(tz => TimeZoneOffsetHelper.GetUtcOffsetString(tz)));
            return response;
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get timezone list");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get timezone list"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting timezone list");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }

    public override async Task<NTPConfig> GetNTPConfig(Empty request, ServerCallContext context)
    {
        try
        {
            var enabled = await _useCase.GetNTPConfigAsync(context.CancellationToken);
            var server = await _useCase.GetNTPServerAsync(context.CancellationToken);
            return new NTPConfig
            {
                IsNTPSynced = enabled,
                Server = server
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get NTP config");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to get NTP config"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting NTP config");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// NTP設定を適用し、設定後の設定値を返す
    /// </summary>
    public override async Task<NTPConfig> ApplyNTPConfig(NTPConfig request, ServerCallContext context)
    {
        try
        {
            await _useCase.SetNTPAsync(request.IsNTPSynced, context.CancellationToken);
            await _useCase.SetNTPServerAsync(request.Server, context.CancellationToken);

            // 設定後の実際のNTP設定を取得して返す
            var enabled = await _useCase.GetNTPConfigAsync(context.CancellationToken);
            var server = await _useCase.GetNTPServerAsync(context.CancellationToken);
            return new NTPConfig
            {
                IsNTPSynced = enabled,
                Server = server
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied to apply NTP config");
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied to apply NTP config"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to apply NTP config");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to apply NTP config"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while applying NTP config");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// NTP同期を実行し、同期結果を返す
    /// </summary>
    public override async Task<NTPConfig> ExecSyncNTP(NTPConfig request, ServerCallContext context)
    {
        try
        {
            // NTP同期を有効にし、NTPサーバーを設定
            await _useCase.SetNTPAsync(request.IsNTPSynced, context.CancellationToken);
            await _useCase.SetNTPServerAsync(request.Server, context.CancellationToken);

            // 現在のNTP設定を取得して返す
            var enabled = await _useCase.GetNTPConfigAsync(context.CancellationToken);
            var server = await _useCase.GetNTPServerAsync(context.CancellationToken);
            return new NTPConfig
            {
                IsNTPSynced = enabled,
                Server = server
            };
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied to execute NTP sync");
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied to execute NTP sync"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to execute NTP sync");
            throw new RpcException(new Status(StatusCode.Internal, "Failed to execute NTP sync"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while executing NTP sync");
            throw new RpcException(new Status(StatusCode.Unknown, "An unexpected error occurred"));
        }
    }
}
