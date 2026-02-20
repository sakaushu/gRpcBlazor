using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.DbusInterfaces;
using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces;

[DBusInterface("org.freedesktop.NetworkManager.Settings.Connection")]
public interface ISettingsConnection : IDBusObject
{
    Task<T> GetAsync<T>(string property);
    Task<IDictionary<string, IDictionary<string, object>>> GetSettingsAsync();
    Task UpdateAsync(IDictionary<string, IDictionary<string, object>> settings);
}
