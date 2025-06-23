using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;

namespace CryptoGuard.MAUI.ViewModels;

public partial class AddCoinPopupViewModel : ObservableObject
{
    private readonly IPortfolioService _portfolioService;
    private readonly ICoinLoreService _coinService;
    public event Action? OnCompleted;

    [ObservableProperty]
    private ObservableCollection<Coin> coins = new();
    
    private Coin? selectedCoin;
    public Coin? SelectedCoin
    {
        get => selectedCoin;
        set
        {
            SetProperty(ref selectedCoin, value);
            OnPropertyChanged(nameof(SelectedCoin));
            if (value != null)
            {
                BuyPrice = value.CurrentPrice;
                BuyPriceText = value.CurrentPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    [ObservableProperty]
    private decimal amount;

    private string amountText = string.Empty;
    public string AmountText
    {
        get => amountText;
        set
        {
            if (SetProperty(ref amountText, value))
            {
                if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    Amount = d;
                else
                    Amount = 0;
            }
        }
    }

    [ObservableProperty]
    private decimal buyPrice;

    private string buyPriceText = string.Empty;
    public string BuyPriceText
    {
        get => buyPriceText;
        set
        {
            if (SetProperty(ref buyPriceText, value))
            {
                if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    BuyPrice = d;
                else
                    BuyPrice = 0;
            }
        }
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ISeries[] priceHistorySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] priceHistoryAxes = Array.Empty<Axis>();

    public bool CanAdd => Amount > 0 && BuyPrice > 0;

    public ICommand AddCommand { get; }

    public AddCoinPopupViewModel(IPortfolioService portfolioService, ICoinLoreService coinService)
    {
        _portfolioService = portfolioService;
        _coinService = coinService;
        _ = LoadCoins();
        AmountText = Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        BuyPriceText = BuyPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
        AddCommand = new AsyncRelayCommand(AddCoin);
    }

    private async Task LoadCoins()
    {
        try
        {
            IsBusy = true;
            var coinList = await _coinService.GetTopCoinsAsync(100);
            Coins = new ObservableCollection<Coin>(coinList);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddCoin()
    {
        if (SelectedCoin == null)
        {
            await Shell.Current.DisplayAlert("Hata", "Lütfen bir coin seçin.", "Tamam");
            return;
        }
        if (Amount <= 0)
        {
            await Shell.Current.DisplayAlert("Hata", "Lütfen geçerli bir miktar girin.", "Tamam");
            return;
        }
        if (BuyPrice <= 0)
        {
            await Shell.Current.DisplayAlert("Hata", "Lütfen geçerli bir alış fiyatı girin.", "Tamam");
            return;
        }
        try
        {
            IsBusy = true;
            if (UserContext.CurrentUserId == null)
            {
                await Shell.Current.DisplayAlert("Hata", "Kullanıcı oturumu bulunamadı.", "Tamam");
                return;
            }
            var portfolio = await _portfolioService.GetPortfolio(UserContext.CurrentUserId.Value);
            if (portfolio == null)
            {
                await Shell.Current.DisplayAlert("Hata", "Portföy bulunamadı.", "Tamam");
                return;
            }
            await _portfolioService.AddCoinToPortfolioAsync(portfolio.Id, SelectedCoin.Id, Amount, BuyPrice);
            System.Diagnostics.Debug.WriteLine($"COIN EKLENDİ: {SelectedCoin.Name} - {SelectedCoin.Id} - {Amount} - {BuyPrice}");
            OnCompleted?.Invoke();
            await Shell.Current.DisplayAlert("Başarılı", $"{SelectedCoin.Name} portföye eklendi.", "Tamam");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Coin eklenirken hata oluştu: {ex.Message}", "Tamam");
            System.Diagnostics.Debug.WriteLine($"COIN EKLEME HATASI: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadCoins();
            return;
        }

        var filteredCoins = Coins.Where(c => 
            c.Name.Contains(value, StringComparison.OrdinalIgnoreCase) || 
            c.Symbol.Contains(value, StringComparison.OrdinalIgnoreCase));
        Coins = new ObservableCollection<Coin>(filteredCoins);
    }

    private async Task<List<(DateTime, decimal)>> GetCoinGeckoHistoryAsync(string coinId, int days = 30)
    {
        try
        {
            using var client = new HttpClient();
            var url = $"https://api.coingecko.com/api/v3/coins/{coinId}/market_chart?vs_currency=usd&days={days}";
            var response = await client.GetStringAsync(url);
            var json = System.Text.Json.JsonDocument.Parse(response);
            var prices = json.RootElement.GetProperty("prices");
            var result = new List<(DateTime, decimal)>();
            foreach (var price in prices.EnumerateArray())
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(price[0].GetInt64()).DateTime;
                var value = price[1].GetDecimal();
                result.Add((timestamp, value));
            }
            return result;
        }
        catch
        {
            return new List<(DateTime, decimal)>();
        }
    }

    private async void UpdatePriceHistory()
    {
        try
        {
            if (SelectedCoin == null) return;
            var history = await GetCoinGeckoHistoryAsync(SelectedCoin.Id, 30);
            var points = history.Select(x => new DateTimePoint(x.Item1, (double)x.Item2)).ToList();
            PriceHistorySeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Values = points,
                    Name = SelectedCoin.Name,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometrySize = 0
                }
            };
            PriceHistoryAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Zaman",
                    LabelsRotation = 0,
                    TextSize = 12,
                    Labeler = (value) => DateTimeOffset.FromUnixTimeMilliseconds((long)value).DateTime.ToString("dd/MM")
                },
                new Axis
                {
                    Name = "Fiyat (USD)",
                    Labeler = (value) => $"${value:F2}",
                    TextSize = 12
                }
            };
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Fiyat geçmişi yüklenirken hata oluştu: {ex.Message}", "Tamam");
        }
    }

    partial void OnAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(CanAdd));
    }
    partial void OnBuyPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(CanAdd));
    }
} 