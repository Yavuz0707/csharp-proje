using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Models;
using CryptoGuard.Services.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class NewsViewModel : ObservableObject
    {
        private readonly NewsService _newsService = new NewsService();

        [ObservableProperty]
        private ObservableCollection<NewsItem> news = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public NewsViewModel()
        {
            _ = LoadNewsAsync();
        }

        [RelayCommand]
        public async Task LoadNewsAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var items = await _newsService.GetLatestNewsAsync();
                News = new ObservableCollection<NewsItem>(items);
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Haberler yüklenemedi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
} 