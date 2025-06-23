using CryptoGuard.MAUI.ViewModels;

namespace CryptoGuard.MAUI.Views;

public partial class AllCoinsPage : ContentPage
{
    public AllCoinsPage(AllCoinsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += async (s, e) =>
        {
            if (viewModel.LoadCoinsCommand.CanExecute(null))
                await viewModel.LoadCoinsCommand.ExecuteAsync(null);
        };
    }

    public AllCoinsPage() : this(App.ServiceProvider.GetRequiredService<AllCoinsViewModel>()) { }

    private string MapToCoinGeckoId(string symbol)
    {
        return symbol.ToLower() switch
        {
            "btc" => "bitcoin",
            "eth" => "ethereum",
            "bnb" => "binancecoin",
            "ada" => "cardano",
            "xrp" => "ripple",
            "doge" => "dogecoin",
            "sol" => "solana",
            "dot" => "polkadot",
            "matic" => "matic-network",
            "atom" => "cosmos",
            "usdt" => "tether",
            // Diğer popüler coinler için eşleme ekleyebilirsin
            _ => symbol.ToLower()
        };
    }

    private async void OnCoinSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CryptoGuard.Core.Models.Coin selectedCoin)
        {
            var geckoId = MapToCoinGeckoId(selectedCoin.Symbol);
            var vm = new CoinDetailViewModel(geckoId, selectedCoin.Name);
            var popup = new CoinDetailPopup { BindingContext = vm };
            await Navigation.PushModalAsync(popup);
            ((CollectionView)sender).SelectedItem = null;
        }
    }
} 