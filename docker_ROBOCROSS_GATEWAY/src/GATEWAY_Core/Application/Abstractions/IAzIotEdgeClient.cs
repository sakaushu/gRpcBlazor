using GATEWAYCore;

namespace GATEWAYCore.Application.Abstractions;

public interface IAzIotEdgeClient
{
    Task<Dictionary<string, string>> GetStatusAsync();
    Task<List<AzIotEdgeModuleInfo>> GetModuleInfoAsync();
    Task<AzIotEdgeConnectionConfig> GetConnectionConfigAsync();
    Task UpdateConnectionConfigAsync(AzIotEdgeConnectionConfig config);
}
