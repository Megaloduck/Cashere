using Cashere.Sync.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Cashere.Sync.Hubs;

// Methods the mobile client calls on the desktop's hub.
public interface IPosSyncHub
{
    Task<ScanResultDto> ScanBarcode(ScanBarcodeRequest request);
    Task<CartMutationResultDto> AddProductToCart(int productId, int quantity);
    Task<CartMutationResultDto> DecrementCartItem(int productId);
    Task<CartMutationResultDto> RemoveCartItem(int productId);
    Task<CartMutationResultDto> ClearCart();
    Task<CartDto> GetCurrentCart();
    Task<PaymentOptionsDto> GetPaymentOptions();
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
    Task PaymentNotification(PaymentNotificationDto notification);
}
