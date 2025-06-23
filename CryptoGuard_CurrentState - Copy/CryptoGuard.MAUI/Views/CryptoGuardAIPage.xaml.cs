using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views;

public partial class CryptoGuardAIPage : ContentPage
{
    public CryptoGuardAIPage(CryptoGuardAIViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 