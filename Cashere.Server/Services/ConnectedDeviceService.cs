using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;

namespace Cashere.Server.Services;

// In-memory registry of currently connected mobile clients, keyed by SignalR
// ConnectionId. Populated exclusively by PosSyncHub.OnConnectedAsync /
// OnDisconnectedAsync - nothing else should call RegisterConnected/
// RegisterDisconnected. Registered as a singleton, same lifetime as
// ActiveCartService.
public class ConnectedDeviceService : IConnectedDeviceService
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ConnectedDeviceInfo> _devices = new();

    public event Action? DevicesChanged;

    public IReadOnlyList<ConnectedDeviceInfo> GetConnectedDevices()
    {
        lock (_lock)
        {
            return _devices.Values
                .OrderByDescending(d => d.ConnectedAt)
                .ToList();
        }
    }

    public void RegisterConnected(string connectionId, string? deviceName, string ipAddress)
    {
        lock (_lock)
        {
            _devices[connectionId] = new ConnectedDeviceInfo(
                connectionId,
                string.IsNullOrWhiteSpace(deviceName) ? "Unknown device" : deviceName,
                ipAddress,
                DateTime.UtcNow);
        }

        DevicesChanged?.Invoke();
    }

    public void RegisterDisconnected(string connectionId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _devices.Remove(connectionId);
        }

        if (removed)
        {
            DevicesChanged?.Invoke();
        }
    }
}