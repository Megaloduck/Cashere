using System;
using System.Threading.Tasks;
using Cashere.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cashere.ViewModels.Mobile;

public partial class PaymentViewModel : ViewModelBase
{
    private readonly IPosSyncClientService _syncClient;

    [ObservableProperty] private SyncConnectionState _state = SyncConnectionState.Disconnected;
    [ObservableProperty] private decimal _cartSubtotal;
    [ObservableProperty] private bool _cashEnabled;
    [ObservableProperty] private bool _qrisEnabled;
    [ObservableProperty] private bool _edcEnabled;
    [ObservableProperty] private string? _qrisAccountInfo;
    [ObservableProperty] private string? _edcAccountInfo;
    [ObservableProperty] private SyncPaymentNotification? _lastPaymentNotification;
    [ObservableProperty] private string? _errorMessage;

    public bool IsConnected => State == SyncConnectionState.Connected;
    public bool HasQrisAccountInfo => !string.IsNullOrWhiteSpace(QrisAccountInfo);
    public bool HasEdcAccountInfo => !string.IsNullOrWhiteSpace(EdcAccountInfo);
    public bool HasPaymentNotification => LastPaymentNotification is not null;

    public PaymentViewModel(IPosSyncClientService syncClient)
    {
        _syncClient = syncClient;
        State = syncClient.State;
        syncClient.StateChanged += HandleStateChanged;
        syncClient.CartUpdated += HandleCartUpdated;
        syncClient.PaymentNotificationReceived += HandlePaymentNotification;
    }

    public async Task RefreshAsync()
    {
        ErrorMessage = null;
        if (!IsConnected) return;

        try
        {
            var options = await _syncClient.GetPaymentOptionsAsync();
            CashEnabled = options.CashEnabled;
            QrisEnabled = options.QrisEnabled;
            EdcEnabled = options.EdcEnabled;
            QrisAccountInfo = options.QrisAccountInfo;
            EdcAccountInfo = options.EdcAccountInfo;
            HandleCartUpdated(await _syncClient.GetCurrentCartAsync());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not load payment information: {ex.Message}";
        }
    }

    private void HandleStateChanged(SyncConnectionState state)
    {
        State = state;
        if (state == SyncConnectionState.Connected)
            _ = RefreshAsync();
    }

    private void HandleCartUpdated(SyncCartSnapshot cart) => CartSubtotal = cart.Subtotal;

    private void HandlePaymentNotification(SyncPaymentNotification notification)
    {
        LastPaymentNotification = notification;
        OnPropertyChanged(nameof(HasPaymentNotification));
    }

    partial void OnQrisAccountInfoChanged(string? value) => OnPropertyChanged(nameof(HasQrisAccountInfo));
    partial void OnEdcAccountInfoChanged(string? value) => OnPropertyChanged(nameof(HasEdcAccountInfo));
    partial void OnLastPaymentNotificationChanged(SyncPaymentNotification? value) => OnPropertyChanged(nameof(HasPaymentNotification));
}
