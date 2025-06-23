using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.RefreshUsername();
            viewModel.LoadDataCommand.Execute(null);
        }
    }
} 