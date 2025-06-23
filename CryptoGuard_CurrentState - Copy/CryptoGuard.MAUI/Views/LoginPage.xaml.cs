using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        public LoginPage() : this(App.ServiceProvider.GetRequiredService<LoginViewModel>()) { }
    }
} 