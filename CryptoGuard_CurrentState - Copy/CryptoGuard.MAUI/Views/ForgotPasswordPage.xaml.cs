namespace CryptoGuard.MAUI.Views;

using CryptoGuard.MAUI.ViewModels;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public ForgotPasswordPage() : this(App.ServiceProvider.GetRequiredService<ForgotPasswordViewModel>()) { }
} 