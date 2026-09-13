using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Admin.Settings;

// Combines the two screens the settings-structure doc groups under "Network":
// local-server bind address/port (Synchronization) and the phones currently
// connected to it (Connected Devices). Kept as two composed child ViewModels
// rather than merged fields, since Cashere.Server already treats them as
// separate concerns (CashereServerHost vs. ConnectedDeviceService) - no need
// to touch either's working internals to put them on one screen.
public partial class NetworkSettingsViewModel : ViewModelBase
{
    public SyncronizationAdminViewModel Synchronization { get; }
    public DevicesAdminViewModel ConnectedDevices { get; }

    public NetworkSettingsViewModel(IShopContextService shopContext, IConnectedDeviceService? connectedDevices)
    {
        Synchronization = new SyncronizationAdminViewModel(shopContext);
        ConnectedDevices = new DevicesAdminViewModel(connectedDevices);
    }

    public async Task InitializeAsync()
    {
        await Synchronization.LoadAsync();
        await ConnectedDevices.LoadAsync();
    }

    public async Task RefreshAsync()
    {
        await Synchronization.LoadAsync();
        ConnectedDevices.RefreshCommand.Execute(null);
    }
}