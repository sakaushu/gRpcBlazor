using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using network;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using Tmds.DBus;

namespace Core.Services;

public class NetworkServiceImpl : NetworkService.NetworkServiceBase
{
    private readonly ILogger<NetworkServiceImpl> _logger;
    private static readonly SemaphoreSlim NmLock = new(1, 1);

    public NetworkServiceImpl(ILogger<NetworkServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task<NetworkInterfaceList> GetNetworkInterfaces(
        Empty request,
        ServerCallContext context)
    {
        _logger.LogInformation("GetNetworkInterfaces started. OS={Os}", RuntimeInformation.OSDescription);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var linux = await GetNetworkInterfacesFromNetworkManagerAsync();
                _logger.LogInformation("Linux NetworkManager returned {Count} interfaces", linux.Interfaces.Count);

                if (linux.Interfaces.Count > 0)
                {
                    return linux;
                }

                _logger.LogInformation("NetworkManager returned no interfaces; falling back to .NET NetworkInterface APIs.");
            }

            var fallback = GetRuntimeNetworkInterfaces();
            _logger.LogInformation("NetworkInterface.GetAllNetworkInterfaces returned {Count} interfaces", fallback.Interfaces.Count);
            return fallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNetworkInterfaces failed");
            return new NetworkInterfaceList();
        }
    }

    private NetworkInterfaceList GetRuntimeNetworkInterfaces()
    {
        var response = new NetworkInterfaceList();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            var interfaceInfo = new NICInfo
            {
                Name = ni.Name,
                Description = ni.Description ?? ni.Name,
                MacAddress = ni.GetPhysicalAddress().ToString(),
                Status = ni.OperationalStatus.ToString(),
                Speed = ni.Speed
            };

            try
            {
                var ipProps = ni.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    interfaceInfo.IpAddresses.Add(addr.Address.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read IP properties for {Name}", ni.Name);
            }

            response.Interfaces.Add(interfaceInfo);
        }

        return response;
    }

    private async Task<NetworkInterfaceList> GetNetworkInterfacesFromNetworkManagerAsync()
    {
        var response = new NetworkInterfaceList();

        await NmLock.WaitAsync();
        try
        {
            using var connection = new Connection(Address.System);
            await connection.ConnectAsync();

            var nm = connection.CreateProxy<INetworkManager>(
                "org.freedesktop.NetworkManager",
                "/org/freedesktop/NetworkManager");

            var devicePaths = await nm.GetDevicesAsync();
            _logger.LogInformation("Connected to NetworkManager via D-Bus; {DeviceCount} devices discovered", devicePaths.Length);
            foreach (var devicePath in devicePaths)
            {
                _logger.LogDebug("Inspecting device path {DevicePath}", devicePath);
                var dev = connection.CreateProxy<INetworkDevice>(
                    "org.freedesktop.NetworkManager",
                    devicePath);

                var name = await SafeAsync(() => dev.Interface);
                if (string.IsNullOrEmpty(name) || name == "lo")
                {
                    _logger.LogDebug("Skip interface {Name}", name ?? "(null)");
                    continue;
                }

                var nic = new NICInfo
                {
                    Name = name,
                    Description = name,
                    MacAddress = await SafeAsync(() => dev.HwAddress) ?? string.Empty,
                    Status = NmStateToString(await SafeAsync(() => dev.State)),
                    Speed = (long)(await SafeAsync(() => dev.Speed)) * 1_000_000 // Mb/s -> bps
                };

                await AddIpAddressesAsync(connection, dev, nic);

                _logger.LogDebug("Interface {Name} IPs: {IPs}", nic.Name, string.Join(", ", nic.IpAddresses));
                _logger.LogInformation(
                    "NM D-Bus interface {Name} State={State} MAC={Mac} Speed={Speed}bps IPs=[{IPs}]",
                    nic.Name,
                    nic.Status,
                    nic.MacAddress,
                    nic.Speed,
                    string.Join(", ", nic.IpAddresses));
                response.Interfaces.Add(nic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read interfaces via NetworkManager D-Bus");
        }
        finally
        {
            NmLock.Release();
        }

        return response;
    }

    private static async Task AddIpAddressesAsync(Connection connection, INetworkDevice dev, NICInfo nic)
    {
        var ip4Path = await SafeAsync(() => dev.Ip4Config);
        if (ip4Path != null && ip4Path != ObjectPath.Root)
        {
            var ip4 = connection.CreateProxy<INetworkManagerIP4Config>(
                "org.freedesktop.NetworkManager", ip4Path);
            var addressData = await SafeAsync(() => ip4.AddressData);
            if (addressData != null)
            {
                foreach (var entry in addressData)
                {
                    if (entry.TryGetValue("address", out var addr) && addr is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        nic.IpAddresses.Add(s);
                    }
                }
            }
        }

        var ip6Path = await SafeAsync(() => dev.Ip6Config);
        if (ip6Path != null && ip6Path != ObjectPath.Root)
        {
            var ip6 = connection.CreateProxy<INetworkManagerIP6Config>(
                "org.freedesktop.NetworkManager", ip6Path);
            var addressData = await SafeAsync(() => ip6.AddressData);
            if (addressData != null)
            {
                foreach (var entry in addressData)
                {
                    if (entry.TryGetValue("address", out var addr) && addr is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        nic.IpAddresses.Add(s);
                    }
                }
            }
        }
    }

    private static string NmStateToString(uint? state) => state switch
    {
        100 => "activated",
        50 => "connected (local)",
        40 => "ip config",
        30 => "disconnected",
        20 => "unavailable",
        10 => "down",
        _ => "unknown"
    };

    private static async Task<T?> SafeAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch
        {
            return default;
        }
    }

    [DBusInterface("org.freedesktop.NetworkManager")]
    private interface INetworkManager : IDBusObject
    {
        Task<ObjectPath[]> GetDevicesAsync();
    }

    [DBusInterface("org.freedesktop.NetworkManager.Device")]
    private interface INetworkDevice : IDBusObject
    {
        Task<string> Interface { get; }
        Task<string> HwAddress { get; }
        Task<uint> State { get; }
        Task<uint> Speed { get; }
        Task<ObjectPath> Ip4Config { get; }
        Task<ObjectPath> Ip6Config { get; }
    }

    [DBusInterface("org.freedesktop.NetworkManager.IP4Config")]
    private interface INetworkManagerIP4Config : IDBusObject
    {
        Task<IDictionary<string, object>[]> AddressData { get; }
    }

    [DBusInterface("org.freedesktop.NetworkManager.IP6Config")]
    private interface INetworkManagerIP6Config : IDBusObject
    {
        Task<IDictionary<string, object>[]> AddressData { get; }
    }
}