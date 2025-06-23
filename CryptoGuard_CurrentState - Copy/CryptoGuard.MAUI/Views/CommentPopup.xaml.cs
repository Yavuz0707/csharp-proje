using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views
{
    public partial class CommentPopup : ContentPage
    {
        public CommentPopup(CommentPopupViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
} 