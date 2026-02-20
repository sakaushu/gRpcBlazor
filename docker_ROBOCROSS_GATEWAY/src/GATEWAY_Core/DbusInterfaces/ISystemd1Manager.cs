using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces;

[DBusInterface("org.freedesktop.systemd1.Manager")]
public interface ISystemd1Manager : IDBusObject
{
    Task<ObjectPath> StartUnitAsync(string name, string mode);

    Task<ObjectPath> StopUnitAsync(string name, string mode);

    Task<ObjectPath> GetUnitAsync(string name);

    Task<ObjectPath> RestartUnitAsync(string name, string mode);

    Task<string[]> ListEnvironmentAsync();

    Task SetEnvironmentAsync(string[] environment);

    Task UnsetEnvironmentAsync(string[] environment);
}
