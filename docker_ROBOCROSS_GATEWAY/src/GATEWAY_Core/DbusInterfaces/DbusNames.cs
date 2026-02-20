
using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces;
/// <summary>
/// D-Busインタフェース名前空間
/// </summary>
public static class DbusNames
{
    public const string Service = "org.freedesktop.NetworkManager";
    public const string timedate1 = "org.freedesktop.timedate1";
    public const string systemd1 = "org.freedesktop.systemd1";
    public const string SystemdService = "org.freedesktop.systemd1";
    public const string SystemdPath = "/org/freedesktop/systemd1";
    public static readonly ObjectPath NmRoot = new("/org/freedesktop/NetworkManager");
}
