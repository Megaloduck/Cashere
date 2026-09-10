using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   

namespace Cashere.Services;

// A single mobile client currently connected to this till's SignalR hub.
// DeviceName comes from the client's ?deviceName= query string on connect
// (see SignalRPosSyncClientService) - falls back to "Unknown device" if a
// client didn't send one. IpAddress is read off the underlying HTTP
// connection by the hub, not self-reported, so it can't be spoofed by a
// misbehaving client.
public record ConnectedDeviceInfo(
    string ConnectionId,
    string DeviceName,
    string IpAddress,
    DateTime ConnectedAt);

// Implemented in Cashere.Server, backed by PosSyncHub's OnConnectedAsync/
// OnDisconnectedAsync overrides - same "interface in Cashere, implementation
// in Cashere.Server" split as IProductCatalogChangeNotifier. Both the server
// (which owns the SignalR connections) and the desktop admin UI (which just
// wants to list them) live in the same process, so this is a plain in-memory
// registry rather than anything that needs to cross a network boundary.
public interface IConnectedDeviceService
{
    IReadOnlyList<ConnectedDeviceInfo> GetConnectedDevices();

    // Raised whenever a device connects or disconnects - NOT guaranteed to
    // be raised on the UI thread, since it's invoked directly from SignalR's
    // hub lifecycle callbacks. Subscribers that touch UI-bound collections
    // must marshal onto the UI thread themselves.
    event Action? DevicesChanged;
}