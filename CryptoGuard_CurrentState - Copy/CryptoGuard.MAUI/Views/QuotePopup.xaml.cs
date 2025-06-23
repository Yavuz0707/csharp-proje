using CommunityToolkit.Maui.Views;
using CryptoGuard.MAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Views
{
    public partial class QuotePopup : CommunityToolkit.Maui.Views.Popup
    {
        public QuotePopup(QuotePopupViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            this.Color = Colors.Transparent;
            this.CanBeDismissedByTappingOutsideOfPopup = false;
            vm.CloseRequested += () => MainThread.BeginInvokeOnMainThread(() => Close());
        }
    }
} 