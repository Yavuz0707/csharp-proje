using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class AllCoinsViewModel : ObservableObject
    {
        private readonly ICoinLoreService _coinLoreService;
        private readonly IPortfolioService _portfolioService;

        [ObservableProperty]
        private ObservableCollection<Coin> allCoins = new();

        [ObservableProperty]
        private ObservableCollection<Coin> filteredCoins = new();

        [ObservableProperty]
        private string searchText = string.Empty;

        public AllCoinsViewModel(ICoinLoreService coinLoreService, IPortfolioService portfolioService)
        {
            _coinLoreService = coinLoreService;
            _portfolioService = portfolioService;
            LoadCoinsCommand = new AsyncRelayCommand(LoadCoinsAsync);
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchText))
                    FilterCoins();
            };
        }

        public IAsyncRelayCommand LoadCoinsCommand { get; }

        private async Task LoadCoinsAsync()
        {
            var coins = (await _coinLoreService.GetTopCoinsAsync(500)).ToList();
            var favCoinIds = new List<string>();
            if (UserContext.CurrentUserId != null)
                favCoinIds = await _portfolioService.GetFavoriteCoinsAsync(UserContext.CurrentUserId.Value);
            for (int i = 0; i < coins.Count; i++)
            {
                coins[i].Index = i + 1;
                // CoinGecko CDN'den ikon url'si oluştur (örnek: https://assets.coingecko.com/coins/images/1/large/bitcoin.png)
                // Burada Symbol veya Id'ye göre url oluşturuluyor. En yaygın coinler için çalışır.
                var symbol = coins[i].Symbol.ToLower();
                var id = coins[i].Id.ToLower();
                // En yaygın coinler için özel eşleme
                string imageUrl = symbol switch
                {
                    "btc" => "https://assets.coingecko.com/coins/images/1/large/bitcoin.png",
                    "eth" => "https://assets.coingecko.com/coins/images/279/large/ethereum.png",
                    "usdt" => "https://assets.coingecko.com/coins/images/325/large/Tether.png",
                    "bnb" => "https://assets.coingecko.com/coins/images/825/large/binance-coin-logo.png",
                    "xrp" => "https://assets.coingecko.com/coins/images/44/large/xrp-symbol-white-128.png",
                    "ada" => "https://assets.coingecko.com/coins/images/975/large/cardano.png",
                    "sol" => "https://assets.coingecko.com/coins/images/4128/large/solana.png",
                    "doge" => "https://assets.coingecko.com/coins/images/5/large/dogecoin.png",
                    "usdc" => "https://assets.coingecko.com/coins/images/6319/large/USD_Coin_icon.png",
                    _ => $"https://assets.coingecko.com/coins/images/1/large/bitcoin.png" // fallback: bitcoin
                };
                coins[i].ImageUrl = imageUrl;
                coins[i].IsFavorite = favCoinIds.Contains(coins[i].Id);
                coins[i].ToggleFavoriteCommand = new RelayCommand<Coin>(async (c) => await ToggleFavoriteAsync(c));
            }
            AllCoins = new ObservableCollection<Coin>(coins);
            FilterCoins();
        }

        private void FilterCoins()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCoins = new ObservableCollection<Coin>(AllCoins);
            }
            else
            {
                var filtered = AllCoins.Where(c =>
                    (c.Name?.ToLower().Contains(SearchText.ToLower()) ?? false) ||
                    (c.Symbol?.ToLower().Contains(SearchText.ToLower()) ?? false)).ToList();
                FilteredCoins = new ObservableCollection<Coin>(filtered);
            }
        }

        public async Task ToggleFavoriteAsync(Coin coin)
        {
            if (UserContext.CurrentUserId == null) return;
            int userId = UserContext.CurrentUserId.Value;
            if (coin.IsFavorite)
            {
                await _portfolioService.RemoveFavoriteCoinAsync(userId, coin.Id);
                coin.IsFavorite = false;
            }
            else
            {
                await _portfolioService.AddFavoriteCoinAsync(userId, coin.Id);
                coin.IsFavorite = true;
            }
            OnPropertyChanged(nameof(coin.IsFavorite));
            Microsoft.Maui.Controls.MessagingCenter.Send(this, "FavoritesChanged");
        }
    }
} 