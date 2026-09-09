using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Mobile;

// Placeholder for the product-photo-labeling flow (staff scans a product,
// snaps a photo, uploads it as that product's image). Wired into navigation
// now so the sidebar is complete; the real capture -> preview -> upload flow
// lands once IProductPhotoService and the mobile capture UI are built (step
// 4 of the photo-labeling build order).
public partial class LabelingViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Product photo labeling is coming soon.";
}