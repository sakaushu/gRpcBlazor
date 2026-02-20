using Tmds.DBus;

[DBusInterface("org.freedesktop.systemd1.Unit")]
public interface ISystemd1Unit : IDBusObject
{
    Task<T> GetAsync<T>(string property);
}
