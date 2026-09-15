using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Cashere.ViewModels;

namespace Cashere.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // Lets Enter submit from either field - keyboard-first users on a real
    // till shouldn't have to reach for the mouse just because the pad
    // exists for touch. LoginCommand's generated AsyncRelayCommand already
    // refuses to re-execute while a login is in flight.
    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        if (DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
        {
            vm.LoginCommand.Execute(null);
        }
    }
}