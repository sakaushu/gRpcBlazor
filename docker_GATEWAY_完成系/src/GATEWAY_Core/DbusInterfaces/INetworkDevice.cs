using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces;

[DBusInterface("org.freedesktop.NetworkManager.Device")]
public interface INetworkDevice : IDBusObject
{
    Task<T> GetAsync<T>(string property);
}
