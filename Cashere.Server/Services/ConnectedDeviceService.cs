using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services; 
using Cashere.Server.Hubs;
using Cashere.Sync.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cashere.Server.Services;

// In-memory registry of currently connected mobile clients, keyed by SignalR
// ConnectionId. Populated exclusively by PosSyncHub.OnConnectedAsync /
// OnDisconnectedAsync - nothing else should call RegisterConnected/
// RegisterDisconnected. Registered as a singleton, same lifetime as
// ActiveCartService.
public class ConnectedDeviceService : IConnectedDeviceService
{
    private readonly IHubContext<PosSyncHub, IPosSyncClient> _hubContext;
    private readonly object _lock = new();
    private readonly Dictionary<string, ConnectedDeviceInfo> _devices = new();

    public event Action? DevicesChanged;

    public ConnectedDeviceService(IHubContext<PosSyncHub, IPosSyncClient> hubContext)
    {
        _hubContext = hubContext;
    }

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

    public async Task KickDeviceAsync(string connectionId)
    {
        // Push-based rather than aborting the connection directly: the
        // mobile client's HubConnection uses WithAutomaticReconnect(), which
        // only engages after an *unexpected* disconnect. A raw abort would
        // just look like a dropped connection and reconnect moments later -
        // asking the client to disconnect itself (see
        // SignalRPosSyncClientService's "Kicked" handler) calls its own
        // DisconnectAsync(), an explicit stop automatic reconnect won't
        // override.
        await _hubContext.Clients.Client(connectionId).Kicked("Disconnected by the till administrator.");
    }
}