using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using Cashere.ViewModels.Admin.Settings;

namespace Cashere.Views.Admin.Settings;

public partial class BusinessInfoView : UserControl
{
    private BusinessInfoViewModel? _subscribedViewModel;

    public BusinessInfoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe();

        if (DataContext is BusinessInfoViewModel vm)
        {
            _subscribedViewModel = vm;
            vm.PickLogoRequested += OnPickLogoRequested;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PickLogoRequested -= OnPickLogoRequested;
            _subscribedViewModel = null;
        }
    }

    private async void OnPickLogoRequested()
    {
        if (DataContext is not BusinessInfoViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a logo image",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" } }
            }
        });

        var file = files.FirstOrDefault();
        if (file is null) return;

        await using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        await vm.SetLogoAsync(memory.ToArray(), file.Name);
    }
}
