using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class AdminViewModel : ViewModelBase
{
    public ProductAdminViewModel Products { get; }
    public SupplierAdminViewModel Suppliers { get; }
    public ShopSettingsViewModel Settings { get; }

    public event Action? BackRequested;

    public AdminViewModel(
        IProductAdminService productAdmin,
        ICategoryAdminService categoryAdmin,
        ISupplierAdminService supplierAdmin,
        IShopContextService shopContext)
    {
        Products = new ProductAdminViewModel(productAdmin, categoryAdmin);
        Suppliers = new SupplierAdminViewModel(supplierAdmin);
        Settings = new ShopSettingsViewModel(shopContext);
    }

    public async Task InitializeAsync()
    {
        await Products.LoadAsync();
        await Suppliers.LoadAsync();
        await Settings.LoadAsync();
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();
}