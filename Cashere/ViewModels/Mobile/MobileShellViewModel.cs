using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Services;
using Cashere.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Mobile;

// Top-level navigation host for the mobile app, mirroring how ShellViewModel
// drives the desktop Pos/Admin switch and AdminViewModel drives its own
// sidebar - same ViewLocator-resolves-CurrentView mechanism throughout.
public partial class MobileShellViewModel : ViewModelBase
{
    public PairingViewModel Pairing { get; }
    public ScanningViewModel Scanning { get; }
    public LabelingViewModel Labeling { get; }

    [ObservableProperty]
    private MobileSection _selectedSection = MobileSection.Pairing;

    public ViewModelBase CurrentView => SelectedSection switch
    {
        MobileSection.Pairing => Pairing,
        MobileSection.Scanning => Scanning,
        MobileSection.Labeling => Labeling,
        _ => Pairing
    };

    public MobileShellViewModel(IPosSyncClientService syncClient, IBarcodeScannerService? scanner)
    {
        Pairing = new PairingViewModel(syncClient);
        Scanning = new ScanningViewModel(syncClient, scanner);
        Labeling = new LabelingViewModel();
    }

    public async Task InitializeAsync()
    {
        await Pairing.InitializeAsync();
    }

    partial void OnSelectedSectionChanged(MobileSection value)
    {
        OnPropertyChanged(nameof(CurrentView));

        // Picks up the current cart in case a connection happened on the
        // Pairing tab while Scanning wasn't visible.
        if (value == MobileSection.Scanning)
        {
            _ = Scanning.RefreshAsync();
        }
    }

    [RelayCommand]
    private void SelectSection(MobileSection section) => SelectedSection = section;
}