using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces
{
    [DBusInterface("org.freedesktop.NetworkManager.Connection.Active")]
    public interface IActiveConnection : IDBusObject
    {
        Task<T> GetAsync<T>(string property);
    }
}
