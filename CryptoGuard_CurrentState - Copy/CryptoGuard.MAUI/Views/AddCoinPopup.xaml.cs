using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views;

public partial class AddCoinPopup : ContentPage
{
    public AddCoinPopup(AddCoinPopupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.OnCompleted += async () => await ClosePopup();
    }

    private async Task ClosePopup()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
} 