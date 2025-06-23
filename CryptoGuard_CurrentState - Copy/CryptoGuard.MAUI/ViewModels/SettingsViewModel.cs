using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Threading;
using CryptoGuard.Core.Models;
using Microsoft.Maui.Media;
using CryptoGuard.Core.Interfaces;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        // Temel Ayarlar
        public ObservableCollection<string> ThemeOptions { get; } = new() { "Varsayılan", "Açık", "Koyu" };
        [ObservableProperty] private string selectedTheme = Preferences.Get("Theme", "Varsayılan");

        public ObservableCollection<string> LanguageOptions { get; } = new() { "Türkçe", "English" };
        [ObservableProperty] private string selectedLanguage = Preferences.Get("Language", "Türkçe");

        [ObservableProperty] private bool priceAlertNotification = Preferences.Get("PriceAlertNotification", true);
        [ObservableProperty] private bool newsNotification = Preferences.Get("NewsNotification", true);

        public ObservableCollection<string> DefaultPageOptions { get; } = new() { "Portföy", "Haberler", "Tüm Coinler" };
        [ObservableProperty] private string selectedDefaultPage = Preferences.Get("DefaultPage", "Portföy");

        // Kişisel Ayarlar
        [ObservableProperty] private string username = Preferences.Get("Username", "");
        [ObservableProperty] private string email = Preferences.Get("Email", "");
        [ObservableProperty] private string password = "";
        [ObservableProperty] private string profilePhoto = Preferences.Get("ProfilePhoto", string.Empty);
        [ObservableProperty] private string biography = Preferences.Get("Biography", string.Empty);
        public ICommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        // Portföy ve Coin Ayarları
        public ObservableCollection<string> CurrencyOptions { get; } = new() { "TL", "USD", "EUR" };
        [ObservableProperty] private string selectedCurrency = Preferences.Get("Currency", "TL");
        [ObservableProperty] private string favoriteCoins = Preferences.Get("FavoriteCoins", "");
        public ObservableCollection<string> PortfolioSortOptions { get; } = new() { "Alfabetik", "Miktar", "Değer" };
        [ObservableProperty] private string selectedPortfolioSort = Preferences.Get("PortfolioSort", "Alfabetik");

        // Sesli Komut ve Erişilebilirlik
        [ObservableProperty] private bool voiceCommandEnabled = Preferences.Get("VoiceCommandEnabled", true);
        [ObservableProperty] private bool voiceReadEnabled = Preferences.Get("VoiceReadEnabled", false);
        public ObservableCollection<string> FontSizeOptions { get; } = new() { "Küçük", "Orta", "Büyük" };
        [ObservableProperty] private string selectedFontSize = Preferences.Get("FontSize", "Orta");
        [ObservableProperty] private bool highContrastEnabled = Preferences.Get("HighContrastEnabled", false);

        public IRelayCommand UploadProfilePhotoCommand { get; }
        public IRelayCommand DeleteProfilePhotoCommand { get; }

        [ObservableProperty]
        private bool isPortfolioPublic = Preferences.Get("IsPortfolioPublic", true);

        public SettingsViewModel(IUserService userService)
        {
            _userService = userService;
            SaveProfileCommand = new RelayCommand(async () => await SaveProfile());
            LogoutCommand = new RelayCommand(Logout);
            UploadProfilePhotoCommand = new RelayCommand(async () => await UploadProfilePhoto());
            DeleteProfilePhotoCommand = new RelayCommand(DeleteProfilePhoto);
            LoadCurrentUser();
        }

        private async void LoadCurrentUser()
        {
            if (UserContext.CurrentUserId != null)
            {
                var user = await _userService.GetUserByIdAsync(UserContext.CurrentUserId.Value);
                if (user != null)
                {
                    Username = user.Username;
                    Email = user.Email;
                }
            }
        }

        partial void OnSelectedThemeChanged(string value)
        {
            Preferences.Set("Theme", value);
            Application.Current.UserAppTheme = value switch
            {
                "Açık" => AppTheme.Light,
                "Koyu" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
        partial void OnSelectedLanguageChanged(string value)
        {
            Preferences.Set("Language", value);
            var culture = value == "English" ? "en" : "tr";
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);
            // Gerekirse kullanıcıya "Uygulamayı yeniden başlatın" uyarısı gösterilebilir.
        }
        partial void OnPriceAlertNotificationChanged(bool value) => Preferences.Set("PriceAlertNotification", value);
        partial void OnNewsNotificationChanged(bool value) => Preferences.Set("NewsNotification", value);
        partial void OnSelectedDefaultPageChanged(string value) => Preferences.Set("DefaultPage", value);
        partial void OnUsernameChanged(string value) => Preferences.Set("Username", value);
        partial void OnEmailChanged(string value) => Preferences.Set("Email", value);
        partial void OnSelectedCurrencyChanged(string value) => Preferences.Set("Currency", value);
        partial void OnFavoriteCoinsChanged(string value) => Preferences.Set("FavoriteCoins", value);
        partial void OnSelectedPortfolioSortChanged(string value) => Preferences.Set("PortfolioSort", value);
        partial void OnVoiceCommandEnabledChanged(bool value) => Preferences.Set("VoiceCommandEnabled", value);
        partial void OnSelectedFontSizeChanged(string value) => Preferences.Set("FontSize", value);
        partial void OnHighContrastEnabledChanged(bool value) => Preferences.Set("HighContrastEnabled", value);
        partial void OnVoiceReadEnabledChanged(bool value) => Preferences.Set("VoiceReadEnabled", value);
        partial void OnIsPortfolioPublicChanged(bool value)
        {
            // Artık Preferences.Set kullanılmıyor, değişiklik SaveProfile ile veritabanına kaydedilecek.
        }

        private async Task SaveProfile()
        {
            if (UserContext.CurrentUserId == null)
            {
                await Shell.Current.DisplayAlert("Hata", "Kullanıcı oturumu bulunamadı.", "Tamam");
                return;
            }
            var user = await _userService.GetUserByIdAsync(UserContext.CurrentUserId.Value);
            if (user == null)
            {
                await Shell.Current.DisplayAlert("Hata", "Kullanıcı bulunamadı.", "Tamam");
                return;
            }
            if (!string.IsNullOrWhiteSpace(Username))
                user.Username = Username;
            if (!string.IsNullOrWhiteSpace(Email))
                user.Email = Email;
            if (!string.IsNullOrWhiteSpace(ProfilePhoto))
                user.ProfilePhoto = ProfilePhoto;
            if (!string.IsNullOrWhiteSpace(Biography))
                user.Biography = Biography;
            user.IsPortfolioPublic = IsPortfolioPublic;
            System.Diagnostics.Debug.WriteLine($"SettingsViewModel.SaveProfile: IsPortfolioPublic before update: {user.IsPortfolioPublic}");
            await _userService.UpdateUserAsync(user);
            System.Diagnostics.Debug.WriteLine($"SettingsViewModel.SaveProfile: IsPortfolioPublic after update: {user.IsPortfolioPublic}");
            Preferences.Set("Username", user.Username);
            Preferences.Set("Email", user.Email);
            Preferences.Set("Biography", user.Biography);
            await Shell.Current.DisplayAlert("Başarılı", "Profil bilgileri güncellendi.", "Tamam");
        }

        private async void Logout()
        {
            UserContext.CurrentUserId = null;
            UserContext.CurrentUsername = null;
            await Shell.Current.GoToAsync("LoginPage");
        }

        private async Task UploadProfilePhoto()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Profil fotoğrafı seç",
                    FileTypes = FilePickerFileType.Images
                });
                if (result != null)
                {
                    ProfilePhoto = result.FullPath;
                    Preferences.Set("ProfilePhoto", ProfilePhoto);
                    Microsoft.Maui.Controls.MessagingCenter.Send<object, string>(this, "ProfilePhotoChanged", ProfilePhoto);
                }
            }
            catch { }
        }

        private void DeleteProfilePhoto()
        {
            ProfilePhoto = string.Empty;
            Preferences.Set("ProfilePhoto", string.Empty);
            Microsoft.Maui.Controls.MessagingCenter.Send<object, string>(this, "ProfilePhotoChanged", ProfilePhoto);
        }
    }
} 