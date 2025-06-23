using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Net.Http;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Globalization;
using CommunityToolkit.Maui.Views;
using CryptoGuard.MAUI.Views;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class CoinDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string coinName;
        [ObservableProperty]
        private decimal currentPrice;
        [ObservableProperty]
        private decimal changePercent;
        [ObservableProperty]
        private ISeries[] series = Array.Empty<ISeries>();
        [ObservableProperty]
        private string coinIcon;
        [ObservableProperty]
        private string coinSymbol;
        [ObservableProperty]
        private string marketCap;
        [ObservableProperty]
        private string volume24h;
        [ObservableProperty]
        private string totalSupply;
        [ObservableProperty]
        private string maxSupply;
        [ObservableProperty]
        private string websiteUrl;
        [ObservableProperty]
        private string whitepaperUrl;
        [ObservableProperty]
        private decimal converterBTC = 1;
        [ObservableProperty]
        private decimal converterUSD;
        [ObservableProperty]
        private decimal low24h;
        [ObservableProperty]
        private decimal high24h;
        [ObservableProperty]
        private decimal priceChange24h;
        [ObservableProperty]
        private string converterSymbol;
        [ObservableProperty]
        private ObservableCollection<MarketInfo> markets = new();
        [ObservableProperty]
        private Func<double, string> yLabeler = v => v.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
        [ObservableProperty]
        private Axis[] yAxes;

        public ICommand CloseCommand { get; }
        public ICommand OpenWebsiteCommand { get; }
        public ICommand OpenWhitepaperCommand { get; }
        public ICommand AskAIFuturePriceCommand { get; }
        public ICommand AskAISentimentCommand { get; }
        public ICommand AskAIRandom1Command { get; }
        public ICommand AskAIRandom2Command { get; }

        public string Low24hDisplay => Low24h > 0 ? "$ " + Low24h.ToString("N2") : "-";
        public string High24hDisplay => High24h > 0 ? "$ " + High24h.ToString("N2") : "-";
        public string PriceChange24hDisplay => PriceChange24h != 0 ? (PriceChange24h > 0 ? "+" : "") + PriceChange24h.ToString("F2") + "%" : "-";
        public string FuturePriceButtonText => $"{CoinName}'nin gelecekteki fiyatını ne etkileyebilir?";
        public string SentimentButtonText => $"İnsanlar {CoinName} hakkında ne söylüyor?";
        public string Random1ButtonText => $"{CoinName} ile ilgili en büyük riskler neler?";
        public string Random2ButtonText => $"{CoinName} yatırımcıları için öneriler nelerdir?";

        private static Dictionary<string, string> _exchangeLogos = new();
        private static bool _exchangeLogosLoaded = false;
        private static async Task EnsureExchangeLogosLoadedAsync()
        {
            if (_exchangeLogosLoaded) return;
            using var client = new HttpClient();
            var url = "https://api.coingecko.com/api/v3/exchanges";
            var response = await client.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            foreach (var ex in json.RootElement.EnumerateArray())
            {
                var id = ex.GetProperty("id").GetString();
                var image = ex.GetProperty("image").GetString();
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(image))
                    _exchangeLogos[id] = image;
            }
            _exchangeLogosLoaded = true;
        }

        public CoinDetailViewModel(string coinId, string coinName)
        {
            coinId = MapToCoinGeckoId(coinId);
            CoinName = coinName;
            CoinSymbol = coinId.ToUpper();
            CoinIcon = "bitcoin.png"; // Örnek, dinamikleştirilebilir
            MarketCap = "$2.1T";
            Volume24h = "$35.8B";
            TotalSupply = "19.87M BTC";
            MaxSupply = "21M BTC";
            WebsiteUrl = "https://bitcoin.org";
            WhitepaperUrl = "https://bitcoin.org/bitcoin.pdf";
            Low24h = 105075.33m;
            High24h = 108109.40m;
            ConverterBTC = 1;
            ConverterUSD = 105000; // Örnek
            CloseCommand = new RelayCommand(Close);
            OpenWebsiteCommand = new RelayCommand(OpenWebsite);
            OpenWhitepaperCommand = new RelayCommand(OpenWhitepaper);
            AskAIFuturePriceCommand = new RelayCommand(async () => await AskAIAndShowPopup(FuturePriceButtonText));
            AskAISentimentCommand = new RelayCommand(async () => await AskAIAndShowPopup(SentimentButtonText));
            AskAIRandom1Command = new RelayCommand(async () => await AskAIAndShowPopup(Random1ButtonText));
            AskAIRandom2Command = new RelayCommand(async () => await AskAIAndShowPopup(Random2ButtonText));
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConverterBTC))
                    ConverterUSD = ConverterBTC * CurrentPrice;
                else if (e.PropertyName == nameof(ConverterUSD) && CurrentPrice > 0)
                    ConverterBTC = ConverterUSD / CurrentPrice;
            };
            YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = YLabeler,
                    ShowSeparatorLines = true
                }
            };
            _ = LoadChartAsync(coinId);
        }

        public class MarketInfo
        {
            public int Index { get; set; }
            public string Exchange { get; set; } = string.Empty;
            public string Pair { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string Volume24h { get; set; } = string.Empty;
            public string Confidence { get; set; } = string.Empty;
            public string? LogoUrl { get; set; }
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
                // Diğer popüler coinler için eşleme ekleyebilirsin
                _ => coinId.ToLower()
            };
        }

        private async Task LoadChartAsync(string coinId)
        {
            var (history, _, _, _) = await GetCoinGeckoHistoryAndStatsAsync(coinId, 30);
            Series = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Values = history.Select(x => x.Item2).ToArray(),
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(124, 58, 237, 180), 1.2f),
                    GeometrySize = 0
                }
            };
            if (history.Count > 0)
            {
                CurrentPrice = history.Last().Item2;
                var first = history.First().Item2;
                ChangePercent = (first > 0) ? ((CurrentPrice - first) / first) * 100 : 0;
            }
            await LoadCoinDetailsAsync(coinId);
        }

        private async Task LoadCoinDetailsAsync(string coinId)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                using var client = new HttpClient(handler);
                var url = $"https://api.coingecko.com/api/v3/coins/{coinId}?localization=false&tickers=true&market_data=true&community_data=false&developer_data=false&sparkline=false";
                var response = await client.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var marketData = json.RootElement.GetProperty("market_data");
                Low24h = marketData.GetProperty("low_24h").GetProperty("usd").GetDecimal();
                High24h = marketData.GetProperty("high_24h").GetProperty("usd").GetDecimal();
                PriceChange24h = marketData.GetProperty("price_change_percentage_24h").GetDecimal();
                OnPropertyChanged(nameof(Low24hDisplay));
                OnPropertyChanged(nameof(High24hDisplay));
                OnPropertyChanged(nameof(PriceChange24hDisplay));
                var image = json.RootElement.GetProperty("image");
                CoinIcon = image.GetProperty("large").GetString() ?? image.GetProperty("thumb").GetString() ?? "";
                ConverterSymbol = json.RootElement.GetProperty("symbol").GetString()?.ToUpper() ?? "COIN";
                CoinSymbol = ConverterSymbol;

                // Marketler
                await EnsureExchangeLogosLoadedAsync();
                var tickers = json.RootElement.GetProperty("tickers");
                var marketList = new ObservableCollection<MarketInfo>();
                int idx = 1;
                foreach (var ticker in tickers.EnumerateArray())
                {
                    var market = ticker.GetProperty("market");
                    var exchange = market.GetProperty("name").GetString() ?? "-";
                    var pair = ticker.GetProperty("base").GetString() + "/" + ticker.GetProperty("target").GetString();
                    var price = ticker.GetProperty("last").GetDecimal();
                    var volume = ticker.GetProperty("volume").GetDecimal();
                    var confidence = ticker.TryGetProperty("trust_score", out var trust) ? trust.GetString() : "-";
                    string? logo = null;
                    if (market.TryGetProperty("identifier", out var identifierProp))
                    {
                        var identifier = identifierProp.GetString();
                        if (!string.IsNullOrEmpty(identifier) && _exchangeLogos.TryGetValue(identifier, out var img))
                            logo = img;
                    }
                    marketList.Add(new MarketInfo
                    {
                        Index = idx++,
                        Exchange = exchange,
                        Pair = pair,
                        Price = price,
                        Volume24h = volume.ToString("C0", CultureInfo.GetCultureInfo("en-US")),
                        Confidence = confidence?.ToUpperInvariant() ?? "-",
                        LogoUrl = logo
                    });
                    if (marketList.Count >= 10) break;
                }
                Markets = marketList;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                await Application.Current?.MainPage?.DisplayAlert("API Hatası", "Çok fazla istek attınız, lütfen birkaç saniye bekleyip tekrar deneyin.", "Tamam");
            }
            catch { }
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
                // 24h değişim için market_chart endpoint'inde yok, ayrı çekelim
                decimal change = 0;
                try
                {
                    var url2 = $"https://api.coingecko.com/api/v3/coins/{coinId}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
                    var resp2 = await client.GetStringAsync(url2);
                    var json2 = JsonDocument.Parse(resp2);
                    var marketData = json2.RootElement.GetProperty("market_data");
                    change = marketData.GetProperty("price_change_percentage_24h").GetDecimal();
                    ConverterSymbol = json2.RootElement.GetProperty("symbol").GetString()?.ToUpper() ?? "COIN";
                }
                catch { }
                return (result, min, max, change);
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("API Hatası", ex.Message, "Tamam");
                return (new List<(DateTime, decimal)>(), 0, 0, 0);
            }
        }

        private void Close()
        {
            if (Application.Current?.Windows.FirstOrDefault() is Window window && window.Page is Page page)
            {
                page.Navigation.PopModalAsync();
            }
        }

        private void OpenWebsite()
        {
            if (!string.IsNullOrEmpty(WebsiteUrl))
                Launcher.Default.OpenAsync(WebsiteUrl);
        }
        private void OpenWhitepaper()
        {
            if (!string.IsNullOrEmpty(WhitepaperUrl))
                Launcher.Default.OpenAsync(WhitepaperUrl);
        }

        private async Task AskAIAndShowPopup(string question)
        {
            string aiResponse = await GetRealAICryptoAnswer(question);
            var popup = new AIPopup("CryptoGuardAI", aiResponse);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }

        private async Task<string> GetRealAICryptoAnswer(string question)
        {
            // CryptoGuardAIViewModel'daki GenerateReply fonksiyonunu kullanarak gerçek AI cevabı al
            // Portföy bağımsız, sadece coin sorusu için prompt oluşturulacak
            try
            {
                var apiKey = "V20apE0rnbJLoMBA26bbaNXxxbo33gxsVAKhxJtZ";
                var model = "command";
                string prompt = $"Soru: {question}\nKısa, net, yatırım tavsiyesi vermeden cevapla.";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Cohere-Version", "2022-12-06");
                var requestBody = new {
                    model = model,
                    prompt = prompt,
                    max_tokens = 200,
                    temperature = 0.7,
                    stop_sequences = new string[] { "\n" }
                };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.cohere.ai/v1/generate", content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"Cohere API hatası: {error}";
                }
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var reply = doc.RootElement.GetProperty("generations")[0].GetProperty("text").GetString();
                return reply?.Trim() ?? "Cevap alınamadı.";
            }
            catch (Exception ex)
            {
                return $"Cohere API hatası: {ex.Message}";
            }
        }
    }
} 