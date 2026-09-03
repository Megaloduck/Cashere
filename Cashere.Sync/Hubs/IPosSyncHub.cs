using Cashere.Sync.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Sync.Hubs;

// Methods the mobile client calls on the desktop's hub.
// (Documentation contract - the .NET SignalR client invokes these by
// string name, so this interface doesn't get codegen'd, but keeps the
// server and client sides honest about the shape of each call.)
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
}
