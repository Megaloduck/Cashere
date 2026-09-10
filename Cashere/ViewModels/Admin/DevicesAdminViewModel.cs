using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cashere.ViewModels.Admin;

public partial class DevicesAdminViewModel : ViewModelBase
{
    private readonly IConnectedDeviceService? _connectedDevices;

    public ObservableCollection<ConnectedDeviceInfo> Devices { get; } = new();

    [ObservableProperty]
    private bool _isServiceAvailable;

    public bool HasNoDevices => IsServiceAvailable && Devices.Count == 0;

    public DevicesAdminViewModel(IConnectedDeviceService? connectedDevices)
    {
        _connectedDevices = connectedDevices;
        IsServiceAvailable = connectedDevices is not null;

        if (_connectedDevices is not null)
        {
            // Raised from SignalR's own threads (not the UI thread) whenever
            // a phone connects/disconnects - marshal back onto the UI thread
            // before touching the bound ObservableCollection.
            _connectedDevices.DevicesChanged += () => Dispatcher.UIThread.Post(Refresh);
        }
    }

    public Task LoadAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Refresh()
    {
        if (_connectedDevices is null) return;

        var latest = _connectedDevices.GetConnectedDevices();

        Devices.Clear();
        foreach (var device in latest)
        {
            Devices.Add(device);
        }

        OnPropertyChanged(nameof(HasNoDevices));
    }

    [RelayCommand]
    private async Task Kick(ConnectedDeviceInfo? device)
    {
        if (device is null || _connectedDevices is null) return;

        await _connectedDevices.KickDeviceAsync(device.ConnectionId);

        // Optimistic removal for a snappier feel - the server's
        // RegisterDisconnected also fires DevicesChanged once the phone's
        // own disconnect completes, which just re-confirms the same state.
        Devices.Remove(device);
        OnPropertyChanged(nameof(HasNoDevices));
    }
}