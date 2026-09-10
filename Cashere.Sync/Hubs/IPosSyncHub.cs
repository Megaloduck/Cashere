using Cashere.Sync.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Sync.Hubs;

// Methods the mobile client calls on the desktop's hub.
public interface IPosSyncHub
{
    Task<ScanResultDto> ScanBarcode(ScanBarcodeRequest request);
    Task<CartDto> GetCurrentCart();
}

// Methods the desktop hub pushes to connected mobile clients.
public interface IPosSyncClient
{
    Task CartUpdated(CartDto cart);
    Task ProductCatalogChanged();

    // Pushed when an admin kicks this device from the Devices screen. The
    // client's job on receiving this is to disconnect itself outright (see
    // SignalRPosSyncClientService) rather than treat it as a transient drop
    // that automatic reconnect should paper over.
    Task Kicked(string reason);
}