using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces
{
    [DBusInterface("org.freedesktop.NetworkManager.IP4Config")]
    public interface IIP4Config : IDBusObject
    {
        Task<T> GetAsync<T>(string property);
    }
}
