using CryptoGuard.Core.Models;
using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfileViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        public ProfilePage() : this(App.ServiceProvider.GetRequiredService<ProfileViewModel>()) { }

        private async void OnPostMenuClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton btn && btn.CommandParameter is FeedPost post && BindingContext is ProfileViewModel vm)
            {
                string action = await DisplayActionSheet("Gönderi İşlemleri", "İptal", null, "Düzenle", "Sil");
                if (action == "Sil")
                {
                    if (vm.DeletePostCommand.CanExecute(post))
                        vm.DeletePostCommand.Execute(post);
                }
                else if (action == "Düzenle")
                {
                    if (vm.EditPostCommand.CanExecute(post))
                        vm.EditPostCommand.Execute(post);
                }
            }
        }
    }
} 