namespace CryptoGuard.MAUI.Views;

using CryptoGuard.MAUI.ViewModels;

public partial class ResetPasswordPage : ContentPage
{
    public ResetPasswordPage(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public ResetPasswordPage() : this(App.ServiceProvider.GetRequiredService<ResetPasswordViewModel>()) { }
} 