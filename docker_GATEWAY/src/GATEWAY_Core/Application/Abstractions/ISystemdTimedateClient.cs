namespace GATEWAYCore.Application.Abstractions;

public interface ISystemdTimedateClient
{
    Task<long> GetSystemTimeAsync(CancellationToken ct);
    Task ApplySystemTimeAsync(DateTime dateTime, CancellationToken ct);
    Task<List<string>> ListTimezonesAsync(CancellationToken ct);
    Task<string> GetTimezoneAsync(CancellationToken ct);
    Task SetTimezoneAsync(string timezone, CancellationToken ct);
    Task<bool> GetNTPConfigAsync(CancellationToken ct);
    Task<string> GetNTPServerAsync(CancellationToken ct);
    Task SetNTPAsync(bool enable, CancellationToken ct);
    Task RestartTimesyncServiceAsync(CancellationToken ct);
}
