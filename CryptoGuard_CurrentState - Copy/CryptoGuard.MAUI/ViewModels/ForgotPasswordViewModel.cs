using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class ForgotPasswordViewModel : BaseViewModel
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private string? username;

        [ObservableProperty]
        private string? email;

        [ObservableProperty]
        private string? errorMessage;

        public bool IsNotBusy => !IsBusy;

        public ForgotPasswordViewModel(IUserService userService)
        {
            _userService = userService;
            Title = "Şifre Yenileme";
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Lütfen kullanıcı adı ve e-posta adresinizi giriniz.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Burada kullanıcı adı ve e-posta kontrolü yapılacak
                var user = await _userService.GetUserByUsernameAndEmail(Username, Email);

                if (user != null)
                {
                    await Shell.Current.GoToAsync($"///ResetPasswordPage?userId={user.Id}");
                }
                else
                {
                    ErrorMessage = "Kullanıcı adı veya e-posta adresi hatalı.";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Hata", ex.Message, "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToLoginAsync()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
} 