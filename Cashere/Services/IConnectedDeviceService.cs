using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   

namespace Cashere.Services;

public record ConnectedDeviceInfo(
    string ConnectionId,
    string DeviceName,
    string IpAddress,
    DateTime ConnectedAt);

public interface IConnectedDeviceService
{
    IReadOnlyList<ConnectedDeviceInfo> GetConnectedDevices();

    event Action? DevicesChanged;

    // Tells the target device to disconnect itself (a "soft kick" pushed
    // over the hub, not a raw connection abort). SignalR's automatic
    // reconnect on the mobile side only fires after an *unexpected*
    // closure, so an abort here would just look like a dropped connection
    // and the phone would reconnect moments later - having the client
    // voluntarily call its own DisconnectAsync() is what actually makes a
    // kick stick.
    Task KickDeviceAsync(string connectionId);
}