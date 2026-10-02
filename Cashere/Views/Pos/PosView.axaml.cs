using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Cashere.ViewModels.Pos;

namespace Cashere.Views.Pos;

public partial class PosView : UserControl
{
    private CustomerDisplayWindow? _customerDisplay;
    private PosViewModel? _viewModel;
    private bool _isAttached;

    public PosView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            _isAttached = true;
            UpdateCustomerDisplay();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _isAttached = false;
            CloseCustomerDisplay();
        };
        DataContextChanged += (_, _) => AttachViewModel(DataContext as PosViewModel);
        AttachViewModel(DataContext as PosViewModel);
    }

    private void AttachViewModel(PosViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        UpdateCustomerDisplay();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PosViewModel.EnableCustomerDisplay))
            UpdateCustomerDisplay();
    }

    private void UpdateCustomerDisplay()
    {
        var viewModel = _viewModel;
        if (!_isAttached || viewModel?.EnableCustomerDisplay != true)
        {
            CloseCustomerDisplay();
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        var screen = owner?.Screens.All.FirstOrDefault(candidate => !candidate.IsPrimary);
        if (owner is null || screen is null)
        {
            CloseCustomerDisplay();
            return;
        }

        if (_customerDisplay is not null)
        {
            if (_customerDisplay.Owner == owner && _customerDisplay.DataContext == viewModel.Cart)
                return;

            CloseCustomerDisplay();
        }

        var display = new CustomerDisplayWindow
        {
            DataContext = viewModel.Cart,
            Position = screen.Bounds.Position,
            WindowState = WindowState.Maximized
        };
        _customerDisplay = display;
        display.Closed += (_, _) =>
        {
            if (ReferenceEquals(_customerDisplay, display))
                _customerDisplay = null;
        };
        display.Show(owner);
    }

    private void CloseCustomerDisplay()
    {
        var display = _customerDisplay;
        _customerDisplay = null;
        display?.Close();
    }
}
