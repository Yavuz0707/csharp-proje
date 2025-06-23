using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views
{
    public partial class TrendsPage : ContentPage
    {
        public TrendsPage(TrendsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
} 