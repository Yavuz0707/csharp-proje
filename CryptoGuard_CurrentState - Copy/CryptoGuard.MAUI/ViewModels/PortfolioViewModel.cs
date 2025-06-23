using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using CryptoGuard.MAUI.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Drawing;
using SkiaSharp;
using LiveChartsCore.Defaults;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;

namespace CryptoGuard.MAUI.ViewModels;

public partial class PortfolioViewModel : BaseViewModel
{
    private readonly IPortfolioService _portfolioService;
    private readonly ICoinLoreService _coinGeckoService;

    [ObservableProperty]
    private decimal totalPortfolioValue;

    [ObservableProperty]
    private ObservableCollection<PortfolioItemViewModel> portfolioItems;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int coinCount;

    [ObservableProperty]
    private double totalPortfolioChangePercent;

    [ObservableProperty]
    private bool isListening;

    [ObservableProperty]
    private IEnumerable<ISeries> pieSeries;

    [ObservableProperty]
    private IEnumerable<ISeries> lineSeries;

    [ObservableProperty]
    private decimal totalProfit;

    [ObservableProperty]
    private double totalProfitPercent;

    [ObservableProperty]
    private IEnumerable<ISeries> candlestickSeries;

    [ObservableProperty]
    private decimal totalBalanceUSD = 932128;
    [ObservableProperty]
    private decimal totalBalanceBTC = 19.4967m;
    [ObservableProperty]
    private decimal income = 1331;
    [ObservableProperty]
    private decimal expense = 234;

    [ObservableProperty]
    private IEnumerable<ISeries> btcMiniSeries = new ISeries[]
    {
        new LineSeries<double> { Values = new double[] { 42000, 43000, 43500, 44000, 43700, 43577 }, Stroke = new SolidColorPaint(SKColors.Gold, 2), GeometrySize = 0 }
    };
    [ObservableProperty]
    private IEnumerable<ISeries> ethMiniSeries = new ISeries[]
    {
        new LineSeries<double> { Values = new double[] { 3000, 3200, 3400, 3500, 3450, 34357 }, Stroke = new SolidColorPaint(SKColors.MediumPurple, 2), GeometrySize = 0 }
    };
    [ObservableProperty]
    private decimal btcPrice = 43577;
    [ObservableProperty]
    private decimal ethPrice = 34357;
    [ObservableProperty]
    private double btcChange = 0.27;
    [ObservableProperty]
    private double ethChange = -0.15;

    [ObservableProperty]
    private List<string> currencyOptions = new() { "USD", "BTC" };
    [ObservableProperty]
    private string selectedCurrency = "USD";
    [ObservableProperty]
    private List<string> periodOptions = new() { "24h", "7d", "1m", "1y" };
    [ObservableProperty]
    private string selectedPeriod = "24h";

    [ObservableProperty]
    private string selectedCoinName = "Bitcoin";
    [ObservableProperty]
    private decimal selectedCoinPrice = 43577;
    [ObservableProperty]
    private double selectedCoinChange = 0.27;
    [ObservableProperty]
    private IEnumerable<ISeries> selectedCoinSeries = new ISeries[]
    {
        new LineSeries<double> { Values = new double[] { 42000, 43000, 43500, 44000, 43700, 43577 }, Stroke = new SolidColorPaint(SKColors.Gold, 2), GeometrySize = 0 }
    };

    [ObservableProperty]
    private IEnumerable<ISeries> selectedCoinCandlestickSeries;

    private decimal usdToBtcRate = 43577m; // Örnek oran

    [ObservableProperty]
    private IEnumerable<Axis> mainChartAxes = new Axis[]
    {
        new Axis
        {
            Name = "Zaman",
            LabelsRotation = 0,
            TextSize = 12,
            NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18)
        }
    };

    [ObservableProperty]
    private IEnumerable<Axis> mainChartYAxes = new Axis[]
    {
        new Axis
        {
            Name = "Tutar (USD)",
            Labeler = (value) => string.Format("${0:N2}", value),
            TextSize = 12,
            MinStep = 100,
            ForceStepToMin = true,
            NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18)
        }
    };

    [ObservableProperty]
    private bool showInTL = false;

    [ObservableProperty]
    private decimal usdToTryRate = 32.5m; // Sabit kur, istenirse API'dan alınabilir

    [ObservableProperty]
    private List<string> chartTypeOptions = new() { "Alış", "Satış", "Toplam" };
    [ObservableProperty]
    private string selectedChartType = "Toplam";

    [ObservableProperty]
    private ObservableCollection<PortfolioItemViewModel> favoriteCoins = new();

    public string TotalBalanceDisplay =>
        ShowInTL
            ? $"{(PortfolioItems?.Sum(x => x.CurrentValue) ?? 0) * UsdToTryRate:N2} ₺"
            : $"$ {(PortfolioItems?.Sum(x => x.CurrentValue) ?? 0):N2}";

    public string CurrencyButtonText => ShowInTL ? "TRY" : "USD";

    public IRelayCommand<string> SelectCoinCommand { get; }
    public ICommand EditCoinCommand { get; }
    public ICommand DeleteCoinCommand { get; }

    public PortfolioViewModel(IPortfolioService portfolioService, ICoinLoreService coinGeckoService)
    {
        _portfolioService = portfolioService;
        _coinGeckoService = coinGeckoService;
        PortfolioItems = new ObservableCollection<PortfolioItemViewModel>();
        SelectCoinCommand = new RelayCommand<string>(OnSelectCoin);
        EditCoinCommand = new AsyncRelayCommand<PortfolioItemViewModel>(EditCoin);
        DeleteCoinCommand = new AsyncRelayCommand<PortfolioItemViewModel>(DeleteCoin);
        UpdateSelectedCoin("Bitcoin");
    }

    private void OnSelectCoin(string coinName)
    {
        UpdateSelectedCoin(coinName);
    }

    private void UpdateSelectedCoin(string coinName)
    {
        if (coinName == "Bitcoin")
        {
            SelectedCoinName = "Bitcoin";
            SelectedCoinPrice = btcPrice;
            SelectedCoinChange = btcChange;
            UpdateSelectedCoinCandlestickSeries("BTC");
        }
        else if (coinName == "Ethereum")
        {
            SelectedCoinName = "Ethereum";
            SelectedCoinPrice = ethPrice;
            SelectedCoinChange = ethChange;
            UpdateSelectedCoinCandlestickSeries("ETH");
        }
    }

    private string MapToCoinGeckoId(string coinId)
    {
        return coinId.ToLower() switch
        {
            "btc" => "bitcoin",
            "bitcoin" => "bitcoin",
            "eth" => "ethereum",
            "ethereum" => "ethereum",
            "bnb" => "binancecoin",
            "binance coin" => "binancecoin",
            "ada" => "cardano",
            "cardano" => "cardano",
            "xrp" => "ripple",
            "ripple" => "ripple",
            "doge" => "dogecoin",
            "dogecoin" => "dogecoin",
            "sol" => "solana",
            "solana" => "solana",
            "dot" => "polkadot",
            "polkadot" => "polkadot",
            "matic" => "matic-network",
            "atom" => "cosmos",
            "cosmos" => "cosmos",
            "usdt" => "tether",
            "tether" => "tether",
            _ => coinId.ToLower()
        };
    }

    private List<DateTime> GetDateRange(DateTime start, DateTime end)
    {
        var dates = new List<DateTime>();
        for (var date = start; date <= end; date = date.AddDays(1))
            dates.Add(date);
        return dates;
    }

    private async void UpdateSelectedCoinCandlestickSeries(string coinSymbol = "BTC")
    {
        try
        {
            int days = GetDaysFromPeriod(SelectedPeriod);
            bool showInTRY = ShowInTL;
            decimal usdToTry = UsdToTryRate;
            int userId = UserContext.CurrentUserId ?? 0;
            var history = (await _portfolioService.GetTransactionHistoryByUserIdAsync(userId)).OrderBy(h => h.TransactionDate).ToList();
            if (!history.Any())
            {
                SelectedCoinCandlestickSeries = Array.Empty<ISeries>();
                return;
            }
            DateTime startDate = DateTime.UtcNow.Date.AddDays(-days + 1); // Bugün dahil
            DateTime endDate = DateTime.UtcNow.Date;
            var filtered = history.Where(h => h.TransactionDate.Date >= startDate && h.TransactionDate.Date <= endDate).ToList();
            var allDates = GetDateRange(startDate, endDate);
            var totalSeries = new List<DateTimePoint>();
            decimal cumulativeBuy = 0;
            decimal cumulativeSell = 0;
            foreach (var date in allDates)
            {
                var todays = filtered.Where(h => h.TransactionDate.Date == date);
                decimal buy = 0, sell = 0;
                if (SelectedCurrency == "USD")
                {
                    buy = todays.Where(x => x.TransactionType == TransactionType.Buy).Sum(x => x.Quantity * x.Price);
                    sell = todays.Where(x => x.TransactionType == TransactionType.Sell).Sum(x => x.Quantity * x.Price);
                }
                else if (SelectedCurrency == "TRY")
                {
                    buy = todays.Where(x => x.TransactionType == TransactionType.Buy).Sum(x => x.Quantity * x.Price * usdToTry);
                    sell = todays.Where(x => x.TransactionType == TransactionType.Sell).Sum(x => x.Quantity * x.Price * usdToTry);
                }
                else if (SelectedCurrency == "BTC")
                {
                    buy = todays.Where(x => x.TransactionType == TransactionType.Buy).Sum(x => (x.Quantity * x.Price) / usdToBtcRate);
                    sell = todays.Where(x => x.TransactionType == TransactionType.Sell).Sum(x => (x.Quantity * x.Price) / usdToBtcRate);
                }
                cumulativeBuy += buy;
                cumulativeSell += sell;
                decimal total = cumulativeBuy - cumulativeSell;
                totalSeries.Add(new DateTimePoint(date, (double)total));
            }
            // Y ekseni için min/max ve step hesapla
            var allValues = totalSeries.Select(p => p.Value).ToList();
            double? minY = allValues.Any() ? allValues.Min() : (double?)null;
            double? maxY = allValues.Any() ? allValues.Max() : (double?)null;
            if (minY.HasValue && maxY.HasValue && minY.Value == maxY.Value) { minY = 0; maxY = maxY.Value + 1; }
            if (minY.HasValue && maxY.HasValue && maxY.Value - minY.Value < 1000) { maxY = minY + 1000; }
            double range = (maxY ?? 1) - (minY ?? 0);
            double step = range / 8.0;
            if (step < 1) step = 1;
            SelectedCoinCandlestickSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint> { Values = totalSeries, Name = "Portföy Değeri", Stroke = new SolidColorPaint(new SKColor(167,139,250,255), 2), GeometrySize = 0 }
            };
            MainChartAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Zaman",
                    LabelsRotation = 0,
                    TextSize = 12,
                    Labeler = SafeDateLabel,
                    NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18)
                }
            };
            MainChartYAxes = new Axis[]
            {
                new Axis
                {
                    Name = showInTRY ? "Tutar (TRY)" : "Tutar (USD)",
                    Labeler = (value) => showInTRY ? string.Format("₺{0:N2}", value) : string.Format("${0:N2}", value),
                    TextSize = 12,
                    MinLimit = minY,
                    MaxLimit = maxY,
                    MinStep = step,
                    ForceStepToMin = true,
                    NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18)
                }
            };
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Grafik yüklenirken hata oluştu: {ex.Message}", "Tamam");
        }
    }

    private int GetDaysFromPeriod(string period)
    {
        return period switch
        {
            "24h" => 1,
            "7d" => 7,
            "1m" => 30,
            "1y" => 365,
            _ => 7
        };
    }

    private async Task<(List<(DateTime, decimal)> history, decimal low, decimal high, decimal change)> GetCoinGeckoHistoryAndStatsAsync(string coinId, int days = 30)
    {
        try
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using var client = new HttpClient(handler);
            var url = $"https://api.coingecko.com/api/v3/coins/{coinId}/market_chart?vs_currency=usd&days={days}";
            var response = await client.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var prices = json.RootElement.GetProperty("prices");
            var result = new List<(DateTime, decimal)>();
            decimal min = decimal.MaxValue, max = decimal.MinValue;
            foreach (var price in prices.EnumerateArray())
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(price[0].GetInt64()).DateTime;
                var value = price[1].GetDecimal();
                result.Add((timestamp, value));
                if (value < min) min = value;
                if (value > max) max = value;
            }
            decimal change = 0;
            try
            {
                var url2 = $"https://api.coingecko.com/api/v3/coins/{coinId}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
                var resp2 = await client.GetStringAsync(url2);
                var json2 = JsonDocument.Parse(resp2);
                var marketData = json2.RootElement.GetProperty("market_data");
                change = marketData.GetProperty("price_change_percentage_24h").GetDecimal();
            }
            catch { }
            return (result, min, max, change);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("API Hatası", ex.Message, "Tamam");
            return (new List<(DateTime, decimal)>(), 0, 0, 0);
        }
    }

    public async Task LoadPortfolioData()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (UserContext.CurrentUserId == null)
            {
                await Shell.Current.DisplayAlert("Hata", "Kullanıcı oturumu bulunamadı.", "Tamam");
                return;
            }
            int userId = UserContext.CurrentUserId.Value;
            var items = await _portfolioService.GetPortfolioItemsByUserIdAsync(userId);
            PortfolioItems.Clear();
            decimal totalValue = 0;
            double weightedChange = 0;

            foreach (var item in items)
            {
                // Get updated coin price from API
                var updatedCoin = await _coinGeckoService.GetCoinPriceAsync(item.CoinId);
                if (updatedCoin == null)
                {
                    // Kullanıcıya hata mesajı göster
                    continue;
                }
                var coinName = updatedCoin?.Name ?? item.CoinId;
                var coinSymbol = updatedCoin?.Symbol ?? string.Empty;
                var coinCapId = ToCoinCapSlug(coinName, coinSymbol);
                var vm = new PortfolioItemViewModel
                {
                    Id = item.Id,
                    PortfolioId = item.PortfolioId,
                    CoinId = item.CoinId,
                    CoinName = coinName,
                    CoinSymbol = coinSymbol,
                    CoinImage = updatedCoin?.ImageUrl,
                    Quantity = item.Quantity,
                    BuyPrice = item.BuyPrice,
                    CurrentPrice = updatedCoin?.CurrentPrice ?? 0,
                    CurrentValue = item.Quantity * (decimal)(updatedCoin?.CurrentPrice ?? 0),
                    ProfitPercent = item.BuyPrice > 0
                        ? (double)(((item.CurrentValue - (item.Quantity * item.BuyPrice)) / (item.Quantity * item.BuyPrice)) * 100M)
                        : 0,
                    CoinCapId = coinCapId
                };
                totalValue += vm.CurrentValue;
                PortfolioItems.Add(vm);
                if (updatedCoin != null && totalValue > 0)
                    weightedChange += (double)(vm.CurrentValue / totalValue) * (double)updatedCoin.PriceChangePercentage24h;
            }
            System.Diagnostics.Debug.WriteLine($"PORTFÖY YÜKLENDİ: {PortfolioItems.Count} coin var.");
            foreach (var item in PortfolioItems)
            {
                System.Diagnostics.Debug.WriteLine($"COIN: {item.CoinName} - {item.CoinId} - {item.Quantity}");
            }
            TotalPortfolioValue = totalValue;
            CoinCount = PortfolioItems.Count;
            TotalPortfolioChangePercent = weightedChange;

            // Update pie chart
            UpdatePieSeries();
            // Update line chart
            UpdateLineSeries();
            // Update profit calculations
            UpdateProfit();
            // Update candlestick chart
            UpdateCandlestickSeries();

            await LoadFavoriteCoinsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Portföy yüklenirken hata oluştu: {ex.Message}", "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddNewItem()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var addCoinVm = new AddCoinPopupViewModel(_portfolioService, _coinGeckoService);
            var popup = new Views.AddCoinPopup(addCoinVm);
            addCoinVm.OnCompleted += async () =>
            {
                await LoadPortfolioData();
                // Main sayfa güncellemesi:
                if (Application.Current.MainPage is Shell shell &&
                    shell.CurrentPage is CryptoGuard.MAUI.Views.MainPage mainPage &&
                    mainPage.BindingContext is MainViewModel mainViewModel)
                {
                    await mainViewModel.LoadDataAsyncCommand.ExecuteAsync(null);
                }
            };
            await Shell.Current.Navigation.PushModalAsync(popup);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditCoin(PortfolioItemViewModel item)
    {
        decimal eskiMiktar = item.Quantity;
        decimal eskiAlis = item.BuyPrice;
        string miktar = await Shell.Current.DisplayPromptAsync("Düzenle", $"{item.CoinName} miktarını gir:", initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(miktar)) return;
        string alis = await Shell.Current.DisplayPromptAsync("Düzenle", $"{item.CoinName} alış fiyatını gir:", initialValue: item.BuyPrice.ToString(), keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(alis)) return;
        if (!decimal.TryParse(miktar, out decimal yeniMiktar) || !decimal.TryParse(alis, out decimal yeniAlis))
        {
            await Shell.Current.DisplayAlert("Hata", "Geçersiz değer girdiniz.", "Tamam");
            return;
        }
        try
        {
            // Gider olarak eski değer, gelir olarak yeni değer ekle
            Expense += eskiMiktar * eskiAlis;
            Income += yeniMiktar * yeniAlis;
            await _portfolioService.UpdatePortfolioItemAsync(item.Id, yeniMiktar, yeniAlis);
            await LoadPortfolioData();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Güncelleme başarısız: {ex.Message}", "Tamam");
        }
    }

    private async Task DeleteCoin(PortfolioItemViewModel item)
    {
        bool confirm = await Shell.Current.DisplayAlert("Sil", $"{item.CoinName} portföyden silinsin mi?", "Evet", "Hayır");
        if (!confirm) return;
        try
        {
            await _portfolioService.RemoveCoinFromPortfolioAsync(item.PortfolioId, item.CoinId);
            await LoadPortfolioData();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hata", $"Silme işlemi başarısız: {ex.Message}", "Tamam");
        }
    }

    [RelayCommand]
    private async Task ShowCoinDetail(PortfolioItemViewModel item)
    {
        var vm = new CoinDetailViewModel(item.CoinId.ToLower(), item.CoinName);
        var popup = new CoinDetailPopup { BindingContext = vm };
        await Shell.Current.Navigation.PushModalAsync(popup);
    }

    public void UpdatePieSeries()
    {
        var items = PortfolioItems.Where(x => x.CurrentValue > 0).ToList();
        if (items.Count == 0)
        {
            // Show example data if portfolio is empty
            PieSeries = new ISeries[]
            {
                new PieSeries<double> { Name = "Bitcoin (BTC)", Values = new[] { 60.0 }, Fill = new SolidColorPaint(SKColors.DodgerBlue) },
                new PieSeries<double> { Name = "Ethereum (ETH)", Values = new[] { 40.0 }, Fill = new SolidColorPaint(SKColors.MediumPurple) }
            };
            return;
        }
        var colors = new[]
        {
            SKColors.DodgerBlue,
            SKColors.MediumPurple,
            SKColors.LimeGreen,
            SKColors.Orange,
            SKColors.Red,
            SKColors.Yellow,
            SKColors.Cyan,
            SKColors.Magenta,
            SKColors.Gold,
            SKColors.Pink
        };
        var totalValue = items.Sum(x => x.CurrentValue);
        PieSeries = items
            .Select((item, index) => new PieSeries<double>
            {
                Name = $"{item.CoinName} ({item.CoinSymbol})",
                Values = new[] { (double)(item.CurrentValue / Math.Max(totalValue, 1) * 100) },
                Fill = new SolidColorPaint(colors[index % colors.Length]),
                Stroke = null,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = (chartPoint) => $"{item.CoinSymbol}: {chartPoint.Model:F1}%"
            })
            .OrderByDescending(x => x.Values?.FirstOrDefault() ?? 0)
            .ToArray();
    }

    public async void UpdateLineSeries()
    {
        if (UserContext.CurrentUserId == null)
        {
            LineSeries = Array.Empty<ISeries>();
            return;
        }
        int userId = UserContext.CurrentUserId.Value;
        var history = (await _portfolioService.GetTransactionHistoryByUserIdAsync(userId)).OrderBy(h => h.TransactionDate).ToList();
        List<DateTimePoint> points = new();
        if (!history.Any())
        {
            // Example: 7 days of data
            var now = DateTime.Now;
            points.Clear();
            for (int i = 6; i >= 0; i--)
            {
                points.Add(new DateTimePoint(now.AddDays(-i), 1000 + i * 100));
            }
            LineSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Portföy Değeri",
                    Values = points,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometrySize = 8,
                    Fill = null
                }
            };
            MainChartAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Zaman",
                    LabelsRotation = 0,
                    TextSize = 12,
                    NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18),
                    Labeler = SafeDateLabel
                }
            };
            return;
        }
        // Group by date for all history (ignore period filter)
        var grouped = history.GroupBy(h => h.TransactionDate.Date).OrderBy(g => g.Key);
        points.Clear();
        if (SelectedChartType == "Alış")
        {
            double cumulative = 0;
            var allDates = grouped.Select(g => g.Key).ToList();
            if (allDates.Count > 0)
            {
                var start = allDates.First();
                var end = allDates.Last();
                points.Add(new DateTimePoint(start.AddDays(-1), 0));
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var g = grouped.FirstOrDefault(x => x.Key == date);
                    if (g != null)
                    {
                        var totalBuy = g.Where(x => x.TransactionType == TransactionType.Buy).Sum(x => x.TotalAmount);
                        cumulative += (double)totalBuy;
                    }
                    points.Add(new DateTimePoint(date, cumulative));
                }
            }
            LineSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Alış Geçmişi",
                    Values = points,
                    Stroke = new SolidColorPaint(SKColors.Green, 2),
                    GeometrySize = 8,
                    Fill = null
                }
            };
        }
        else if (SelectedChartType == "Satış")
        {
            foreach (var g in grouped)
            {
                var totalSell = g.Where(x => x.TransactionType == TransactionType.Sell).Sum(x => x.TotalAmount);
                points.Add(new DateTimePoint(g.Key, (double)totalSell));
            }
            LineSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Satış Geçmişi",
                    Values = points,
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    GeometrySize = 8,
                    Fill = null
                }
            };
        }
        else // Toplam
        {
            double cumulative = 0;
            var allDates = grouped.Select(g => g.Key).ToList();
            if (allDates.Count > 0)
            {
                var start = allDates.First();
                var end = allDates.Last();
                // Add a starting point at 0 one day before the first transaction
                points.Add(new DateTimePoint(start.AddDays(-1), 0));
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var g = grouped.FirstOrDefault(x => x.Key == date);
                    if (g != null)
                    {
                        var totalBuy = g.Where(x => x.TransactionType == TransactionType.Buy).Sum(x => x.TotalAmount);
                        var totalSell = g.Where(x => x.TransactionType == TransactionType.Sell).Sum(x => x.TotalAmount);
                        cumulative += (double)(totalBuy - totalSell);
                    }
                    points.Add(new DateTimePoint(date, cumulative));
                }
            }
        LineSeries = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = "Portföy Değeri",
                    Values = points,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometrySize = 8,
                    Fill = null
                }
            };
        }
        MainChartAxes = new Axis[]
        {
            new Axis
            {
                Name = "Zaman",
                LabelsRotation = 0,
                TextSize = 12,
                NamePaint = new SolidColorPaint(SKColor.Parse("#b4befe"), 18),
                Labeler = SafeDateLabel
            }
        };
    }

    public void UpdateProfit()
    {
        decimal totalBuy = PortfolioItems.Sum(x => x.BuyValue);
        decimal totalCurrent = PortfolioItems.Sum(x => x.CurrentValue);
        TotalProfit = totalCurrent - totalBuy;
        if (totalBuy > 0)
            TotalProfitPercent = (double)(TotalProfit / totalBuy * 100M);
        else
            TotalProfitPercent = 0;
    }

    public void UpdateCandlestickSeries()
    {
        // Simulate portfolio value over the last 7 days based on transactions if available
        var now = DateTime.Now;
        var candles = new List<FinancialPoint>();
        // If you have a transaction history, you should calculate the portfolio value after each transaction.
        // For now, fallback to current value for all points.
        decimal currentValue = PortfolioItems?.Sum(x => x.CurrentValue) ?? 0;
        for (int i = 6; i >= 0; i--)
        {
            var date = now.AddDays(-i);
            candles.Add(new FinancialPoint(date, (double)currentValue, (double)currentValue, (double)currentValue, (double)currentValue));
        }
        CandlestickSeries = new ISeries[]
        {
            new CandlesticksSeries<FinancialPoint>
            {
                Values = candles,
                Name = "Portföy Değeri",
                UpStroke = new SolidColorPaint(SKColors.LightGreen, 2),
                DownStroke = new SolidColorPaint(SKColors.Red, 2),
                UpFill = null,
                DownFill = null
            }
        };
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        ShowInTL = value == "TRY";
        UpdateSelectedCoinCandlestickSeries();
    }
    partial void OnSelectedPeriodChanged(string value)
    {
        UpdateSelectedCoinCandlestickSeries();
    }

    private string ToCoinCapSlug(string name, string symbol)
    {
        // CoinCap çoğunlukla coin ismini küçük harf ve tireli bekler, bazı istisnalar olabilir.
        // Önce özel eşlemeler, sonra genel slugify
        var lower = name.ToLowerInvariant();
        return lower.Replace(" ", "-").Replace(".", "").Replace(",", "").Replace("'", "").Replace("/", "-");
    }

    partial void OnShowInTLChanged(bool value)
    {
        OnPropertyChanged(nameof(TotalBalanceDisplay));
    }

    partial void OnPortfolioItemsChanged(ObservableCollection<PortfolioItemViewModel> value)
    {
        OnPropertyChanged(nameof(TotalBalanceDisplay));
    }

    partial void OnSelectedChartTypeChanged(string value)
    {
        UpdateLineSeries();
    }

    public IPortfolioService PortfolioService => _portfolioService;
    public ICoinLoreService CoinGeckoService => _coinGeckoService;

    private string SafeDateLabel(double value)
    {
        try
        {
            var v = Convert.ToInt64(value);
            if (v < 100000000000 || v > 253402300799999)
                return "";
            return DateTimeOffset.FromUnixTimeMilliseconds(v).DateTime.ToString("dd.MM");
        }
        catch { return ""; }
    }

    [RelayCommand]
    public async Task AddExampleTransactionsAsync()
    {
        if (UserContext.CurrentUserId == null)
        {
            await Shell.Current.DisplayAlert("Hata", "Kullanıcı oturumu bulunamadı.", "Tamam");
            return;
        }
        int userId = UserContext.CurrentUserId.Value;
        await _portfolioService.InsertExampleTransactionsForUserAsync(userId);
        await LoadPortfolioData();
        await Shell.Current.DisplayAlert("Başarılı", "Örnek işlemler eklendi!", "Tamam");
    }

    public async Task LoadFavoriteCoinsAsync()
    {
        if (UserContext.CurrentUserId == null) return;
        int userId = UserContext.CurrentUserId.Value;
        var favCoinIds = await _portfolioService.GetFavoriteCoinsAsync(userId);
        foreach (var item in PortfolioItems)
        {
            item.IsFavorite = favCoinIds.Contains(item.CoinId);
            item.ToggleFavoriteCommand = new RelayCommand(async () => await ToggleFavoriteAsync(item));
        }
        FavoriteCoins = new ObservableCollection<PortfolioItemViewModel>(PortfolioItems.Where(x => x.IsFavorite));
    }

    public async Task ToggleFavoriteAsync(PortfolioItemViewModel item)
    {
        if (UserContext.CurrentUserId == null) return;
        int userId = UserContext.CurrentUserId.Value;
        if (item.IsFavorite)
        {
            await _portfolioService.RemoveFavoriteCoinAsync(userId, item.CoinId);
            item.IsFavorite = false;
        }
        else
        {
            await _portfolioService.AddFavoriteCoinAsync(userId, item.CoinId);
            item.IsFavorite = true;
        }
        await LoadFavoriteCoinsAsync();
    }
}

public class PortfolioItemViewModel : ObservableObject
{
    public int Id { get; set; } // PortfolioItem'ın veritabanı Id'si
    public int PortfolioId { get; set; } // Portföy Id'si
    public string CoinId { get; set; } = string.Empty;
    public string CoinName { get; set; } = string.Empty;
    public string CoinSymbol { get; set; } = string.Empty;
    public string? CoinImage { get; set; }
    public decimal Quantity { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal BuyValue => Quantity * BuyPrice;
    public decimal Profit => CurrentValue - BuyValue;
    public double Change24h { get; set; }
    public double ProfitPercent { get; set; }
    public string CoinCapId { get; set; } = string.Empty;
    private bool isFavorite;
    public bool IsFavorite
    {
        get => isFavorite;
        set => SetProperty(ref isFavorite, value);
    }
    public IRelayCommand ToggleFavoriteCommand { get; set; }
} 