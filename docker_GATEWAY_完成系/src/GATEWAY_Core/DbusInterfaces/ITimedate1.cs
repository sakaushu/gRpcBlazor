using GATEWAYCore.Application.Abstractions;
using GATEWAYCore.DbusInterfaces;
using Tmds.DBus;

namespace GATEWAYCore.DbusInterfaces;

[DBusInterface("org.freedesktop.timedate1")]
public interface ITimedate1 : IDBusObject
{
    Task SetTimeAsync(long usecUtc, bool relative, bool interactive);
    Task SetTimezoneAsync(string timezone, bool interactive);
    Task SetNTPAsync(bool useNtp, bool interactive);
    Task<string[]> ListTimezonesAsync();
    Task<T> GetAsync<T>(string property);
}
