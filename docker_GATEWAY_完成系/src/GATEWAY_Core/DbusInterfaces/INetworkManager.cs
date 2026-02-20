using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces
{
    [DBusInterface("org.freedesktop.NetworkManager")]
    public interface INetworkManager : IDBusObject
    {
        Task<ObjectPath[]> GetDevicesAsync();
        Task<ObjectPath> ActivateConnectionAsync(ObjectPath connection, ObjectPath device, ObjectPath specificObject);
    }
}
