using System;
using System.Runtime.InteropServices;
using System.Threading;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using systeminfo;
using Tmds.DBus;

namespace Core.Services;

public class SystemServiceImpl : SystemService.SystemServiceBase
{
    private readonly ILogger<SystemServiceImpl> _logger;
    private static readonly SemaphoreSlim TimeLock = new(1, 1);

    public SystemServiceImpl(ILogger<SystemServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task<SystemTime> GetSystemTime(Empty request, ServerCallContext context)
    {
        _logger.LogInformation("GetSystemTime RPC started. OS={Os}", RuntimeInformation.OSDescription);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogInformation("Linux OS detected, attempting D-Bus retrieval...");
                var dbusTime = await GetSystemTimeFromDbusAsync();
                if (dbusTime != null)
                {
                    _logger.LogInformation("Returning system time from D-Bus successfully");
                    return dbusTime;
                }

                _logger.LogWarning("D-Bus retrieval failed or returned null; falling back to .NET runtime clock");
            }
            else
            {
                _logger.LogInformation("Non-Linux OS detected, using .NET runtime clock");
            }

            var now = DateTimeOffset.Now;
            var fallbackResult = BuildSystemTime(now.ToUnixTimeMilliseconds() * 1000, now);
            _logger.LogInformation(
                "Returning system time from fallback (.NET runtime) - UnixMicroseconds={UnixMicroseconds}, ISO8601={Iso}, Source=Fallback",
                fallbackResult.UnixMicroseconds,
                fallbackResult.Iso8601);
            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSystemTime failed with exception: {Message}", ex.Message);
            var now = DateTimeOffset.Now;
            var errorResult = BuildSystemTime(now.ToUnixTimeMilliseconds() * 1000, now);
            _logger.LogWarning(
                "Returning system time from error fallback - UnixMicroseconds={UnixMicroseconds}, ISO8601={Iso}, Source=ErrorFallback",
                errorResult.UnixMicroseconds,
                errorResult.Iso8601);
            return errorResult;
        }
    }

    private async Task<SystemTime?> GetSystemTimeFromDbusAsync()
    {
        await TimeLock.WaitAsync();
        try
        {
            _logger.LogInformation("Attempting to connect to D-Bus system bus...");
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();
            _logger.LogInformation("Successfully connected to D-Bus system bus");

            _logger.LogInformation("Creating proxy for org.freedesktop.timedate1...");
            var timedate = connection.CreateProxy<ITimedate1>(
                "org.freedesktop.timedate1",
                "/org/freedesktop/timedate1");
            _logger.LogInformation("Successfully created timedate1 proxy");

            _logger.LogInformation("Requesting TimeUSec property from D-Bus timedate1...");
            var usec = await SafeAsync(() => timedate.TimeUSec);
            
            if (usec == 0)
            {
                _logger.LogWarning("D-Bus timedate1 returned zero or failed (TimeUSec={TimeUSec})", usec);
                return null;
            }

            _logger.LogInformation("Successfully retrieved TimeUSec from D-Bus: {TimeUSec} microseconds", usec);
            
            var dto = DateTimeOffset.FromUnixTimeMilliseconds(usec / 1000);
            var result = BuildSystemTime(usec, dto);
            
            _logger.LogInformation(
                "D-Bus system time successfully retrieved - UnixMicroseconds={UnixMicroseconds}, ISO8601={Iso}, Source=D-Bus_timedate1",
                result.UnixMicroseconds,
                result.Iso8601);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read system time via timedate1 D-Bus - Exception: {Message}", ex.Message);
            return null;
        }
        finally
        {
            TimeLock.Release();
        }
    }

    private static SystemTime BuildSystemTime(long unixMicroseconds, DateTimeOffset dto)
    {
        return new SystemTime
        {
            UnixMicroseconds = unixMicroseconds,
            Iso8601 = dto.ToString("O")
        };
    }

    private static async Task<T?> SafeAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch
        {
            return default;
        }
    }

    [DBusInterface("org.freedesktop.timedate1")]
    private interface ITimedate1 : IDBusObject
    {
        Task<long> TimeUSec { get; }
    }
}
