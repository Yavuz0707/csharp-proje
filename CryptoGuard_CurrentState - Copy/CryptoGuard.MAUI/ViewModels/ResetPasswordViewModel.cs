using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace CryptoGuard.MAUI.ViewModels
{
    [QueryProperty(nameof(UserId), "userId")]
    public partial class ResetPasswordViewModel : BaseViewModel
    {
        private readonly IUserService _userService;

        [ObservableProperty]
        private int userId;

        [ObservableProperty]
        private string? newPassword;

        [ObservableProperty]
        private string? confirmNewPassword;

        [ObservableProperty]
        private string? errorMessage;

        public bool IsNotBusy => !IsBusy;

        public ResetPasswordViewModel(IUserService userService)
        {
            _userService = userService;
            Title = "Yeni Şifre Belirle";
        }

        [RelayCommand]
        private async Task SetNewPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                ErrorMessage = "Lütfen yeni şifreyi ve şifre onayını giriniz.";
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "Şifreler uyuşmuyor.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var success = await _userService.ResetPasswordWithoutOldPasswordAsync(UserId, NewPassword);
                if (success)
                {
                    await Shell.Current.DisplayAlert("Başarılı", "Şifreniz başarıyla güncellendi. Şimdi giriş yapabilirsiniz.", "Tamam");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    ErrorMessage = "Şifre güncelleme başarısız oldu.";
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