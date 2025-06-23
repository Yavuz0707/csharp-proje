using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IPortfolioService _portfolioService;

        private string? username;
        public string? Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }

        private string? password;
        public string? Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        private string? errorMessage;
        public string? ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public bool IsNotBusy => !IsBusy;

        public LoginViewModel(IUserService userService, IPortfolioService portfolioService)
        {
            _userService = userService;
            _portfolioService = portfolioService;
            Title = "Login";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = "Login butonuna tıklandı!";
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _userService.Login(Username, Password);
                if (user != null)
                {
                    UserContext.CurrentUserId = user.Id;
                    UserContext.CurrentUsername = user.Username;
                    Preferences.Set("CurrentUserId", user.Id);
                    Preferences.Set("CurrentUsername", user.Username);
                    var portfolio = await _portfolioService.GetPortfolio(user.Id);
                    if (portfolio == null)
                    {
                        portfolio = await _portfolioService.CreatePortfolioAsync(new Portfolio
                        {
                            UserId = user.Id,
                            Name = "Varsayılan Portföy"
                        });
                    }
                    // Kullanıcıyı uygulamaya yönlendir
                    await Shell.Current.GoToAsync("//MainPage");
                    var mainViewModel = Shell.Current.CurrentPage?.BindingContext as MainViewModel;
                    mainViewModel?.RefreshUsername();
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
        private async Task RegisterAsync()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }

        [RelayCommand]
        private async Task CloseAsync()
        {
            await Shell.Current.GoToAsync(".."); // Önceki sayfaya dön veya uygulamayı kapat
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            // await Shell.Current.DisplayAlert("Şifre Sıfırlama", "Şifre sıfırlama ekranına yönlendirileceksiniz.", "Tamam");
            await Shell.Current.GoToAsync("//ForgotPasswordPage"); // Şifre sıfırlama sayfasına yönlendir
        }
    }
} 