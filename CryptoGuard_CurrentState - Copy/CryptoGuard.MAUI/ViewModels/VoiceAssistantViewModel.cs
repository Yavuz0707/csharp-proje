using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.MAUI.Services;
using Microsoft.Maui.Controls;
using System.Linq;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class VoiceAssistantViewModel : ObservableObject
    {
        private readonly SpeechService _speechService;
        private readonly ICoinLoreService _coinLoreService;
        private readonly IPortfolioService _portfolioService;

        [ObservableProperty] private bool isListening;
        [ObservableProperty] private string lastCommand = string.Empty;
        [ObservableProperty] private string assistantStatus = "";

        private static readonly Dictionary<string, string> CoinIds = new()
        {
            { "bitcoin", "90" }, { "btc", "90" },
            { "ethereum", "80" }, { "eth", "80" },
            { "cosmos", "3077" }, { "atom", "3077" },
            { "dogecoin", "2" }, { "doge", "2" },
            { "solana", "48543" }, { "sol", "48543" },
            { "cardano", "257" }, { "ada", "257" }
        };

        private bool isPromptingForMore = false;

        private static readonly List<(string Pattern, string Response)> SmallTalkCommands = new()
        {
            ("nasılsın", "Çok iyiyim, teşekkürler! Size nasıl yardımcı olabilirim?"),
            ("teşekkür", "Rica ederim, her zaman buradayım."),
            ("görüşürüz", "Görüşmek üzere! İstediğiniz zaman bana seslenebilirsiniz."),
            ("selam", "Merhaba! Size nasıl yardımcı olabilirim?"),
            ("adın ne", "Ben CryptoGuard sesli asistanıyım."),
            ("şaka yap", "Bir kripto şakası: Bitcoin neden bilgisayarı sevmez? Çünkü çok fazla virüs var!"),
            ("saat kaç", "dynamic_time"),
            ("motive et", "Unutmayın, başarı sabır ve azimle gelir. Siz harikasınız!"),
            ("yardım et", "Size portföyünüzü gösterebilir, coin fiyatlarını söyleyebilir, haberleri okuyabilirim."),
            ("ne yapabilirsin", "Kripto fiyatlarını, portföyünüzü, haberleri ve daha fazlasını size sunabilirim."),
            ("iyi akşamlar", "İyi akşamlar! Size nasıl yardımcı olabilirim?"),
            ("günaydın", "Günaydın! Harika bir gün dilerim."),
            ("hoşça kal", "Hoşça kalın! Görüşmek üzere."),
            ("eyvallah", "Ne demek, her zaman!"),
            ("bana motivasyon ver", "Başarı, vazgeçmeyenlerindir. Siz de başarabilirsiniz!"),
            // ... istediğiniz kadar ekleyin
        };

        public VoiceAssistantViewModel(ICoinLoreService coinLoreService, IPortfolioService portfolioService)
        {
            _speechService = new SpeechService();
            _coinLoreService = coinLoreService;
            _portfolioService = portfolioService;
            _speechService.OnWakeWordDetected += OnWakeWordDetected;
            _speechService.OnCommandRecognized += OnCommandRecognized;
        }

        [RelayCommand]
        public void StartWakeWordListening()
        {
            IsListening = false;
            AssistantStatus = "Asistanı başlatmak için 'asistan' deyin.";
            _speechService.StartWakeWordListening();
        }

        private async void OnWakeWordDetected()
        {
            if (!isPromptingForMore)
            {
                IsListening = true;
                AssistantStatus = "Sizi dinliyorum efendim";
                await TextToSpeech.Default.SpeakAsync("Sizi dinliyorum efendim");
            }
            _speechService.StartCommandListening();
        }

        private async void OnCommandRecognized(string command)
        {
            LastCommand = command;
            IsListening = false;
            AssistantStatus = $"Komut: {command}";

            if (isPromptingForMore)
            {
                // Kullanıcıya tekrar sorulmuştu, şimdi cevabını işle
                if (command.Contains("evet"))
                {
                    isPromptingForMore = false;
                    IsListening = true;
                    AssistantStatus = "Sizi dinliyorum...";
                    StartCommandListeningWithoutWakeWord();
                }
                else if (command.Contains("hayır"))
                {
                    isPromptingForMore = false;
                    await TextToSpeech.Default.SpeakAsync("Tamam efendim, istediğiniz zaman bana seslenebilirsiniz.");
                    // Dinlemeyi durdur
                }
                else
                {
                    // Anlaşılmadı, tekrar sor
                    await TextToSpeech.Default.SpeakAsync("Lütfen evet ya da hayır deyin. Başka bir şey ister misiniz?");
                    StartPromptListening();
                }
                return;
            }

            // Normal komut işleme
            await HandleCommand(command);

            // Komut sonrası tekrar sor
            await Task.Delay(1000);
            await TextToSpeech.Default.SpeakAsync("Başka bir şey ister misiniz?");
            isPromptingForMore = true;
            StartPromptListening();
        }

        private async Task HandleCommand(string command)
        {
            command = command.ToLower().Trim();

            // Small talk ve doğal konuşma komutları
            foreach (var (pattern, response) in SmallTalkCommands)
            {
                if (command.Contains(pattern))
                {
                    if (pattern == "saat kaç")
                        await TextToSpeech.Default.SpeakAsync($"Şu an saat {DateTime.Now:HH:mm}.");
                    else
                        await TextToSpeech.Default.SpeakAsync(response);
                    return;
                }
            }

            // Fiyat sorgusu
            if (Regex.IsMatch(command, @"(fiyat|kaç|ne kadar|değeri|dolar)"))
            {
                var coinKey = CoinIds.Keys.FirstOrDefault(k => command.Contains(k));
                if (coinKey != null)
                {
                    var coinId = CoinIds[coinKey];
                    var coinName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(coinKey);
                    var coin = await _coinLoreService.GetCoinPriceAsync(coinId);
                    if (coin == null)
                    {
                        await TextToSpeech.Default.SpeakAsync($"{coinName} fiyatı alınamadı.");
                        return;
                    }
                    await TextToSpeech.Default.SpeakAsync($"{coinName} şu an {coin.CurrentPrice} dolar.");
                }
                else
                {
                    await TextToSpeech.Default.SpeakAsync("Hangi coinin fiyatını öğrenmek istiyorsunuz?");
                }
                return;
            }

            // Portföy gösterme
            if (Regex.IsMatch(command, @"(portföy|varlıklarım|yatırımım|portfoyumu göster|portföyümü oku)"))
            {
                var userId = UserContext.CurrentUserId;
                if (userId.HasValue)
                {
                    var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId.Value);
                    if (portfolios.Any())
                    {
                        string text = "Portföyünüzde şunlar var: ";
                        foreach (var portfolio in portfolios)
                        {
                            foreach (var item in portfolio.Items)
                            {
                                text += $"{item.CoinId} {item.Quantity} adet, ";
                            }
                        }
                        await TextToSpeech.Default.SpeakAsync(text);
                    }
                    else
                    {
                        await TextToSpeech.Default.SpeakAsync("Portföyünüz boş.");
                    }
                }
                return;
            }

            // Haberler
            if (Regex.IsMatch(command, @"(haber|gündem|son dakika|kripto haber)"))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("//NewsPage");
                });
                await TextToSpeech.Default.SpeakAsync("Kripto haberleri açıldı.");
                return;
            }

            // Grafik
            if (Regex.IsMatch(command, @"(grafik|chart|trend)"))
            {
                await TextToSpeech.Default.SpeakAsync("Grafik özelliği yakında eklenecek.");
                return;
            }

            // Coin ekle
            if (Regex.IsMatch(command, @"(coin ekle|yeni coin|varlık ekle)"))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var vm = App.ServiceProvider.GetService<PortfolioViewModel>();
                    var popupVm = new AddCoinPopupViewModel(vm.PortfolioService, vm.CoinGeckoService);
                    var popup = new CryptoGuard.MAUI.Views.AddCoinPopup(popupVm);
                    await Application.Current.MainPage.Navigation.PushModalAsync(popup);
                });
                await TextToSpeech.Default.SpeakAsync("Coin ekleme penceresi açıldı.");
                return;
            }

            // Ana sayfa
            if (Regex.IsMatch(command, @"(ana sayfa|anasayfa|başlangıç)"))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("//MainPage");
                });
                await TextToSpeech.Default.SpeakAsync("Ana sayfa açıldı.");
                return;
            }

            // Çıkış
            if (Regex.IsMatch(command, @"(çıkış|oturumu kapat|logout|kapat)"))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Preferences.Clear();
                    await Shell.Current.GoToAsync("//LoginPage");
                });
                await TextToSpeech.Default.SpeakAsync("Çıkış yapıldı.");
                return;
            }

            // Yardım
            if (Regex.IsMatch(command, @"(yardım|komutlar|ne yapabilirsin)"))
            {
                await TextToSpeech.Default.SpeakAsync("Örnek komutlar: Bitcoin fiyatı, Portföyümü göster, Ethereum grafiği, Kripto haberleri, Yardım.");
                return;
            }

            // Bilinmeyen komut
            await TextToSpeech.Default.SpeakAsync("Üzgünüm, bu komutu anlayamadım. Yardım diyerek örnek komutları öğrenebilirsiniz.");
        }

        // Wake word olmadan doğrudan komut dinleme başlat
        private void StartCommandListeningWithoutWakeWord()
        {
            _speechService.StartCommandListening();
        }

        // Sadece evet/hayır beklemek için dinleme başlat
        private void StartPromptListening()
        {
            _speechService.StartCommandListening();
        }
    }
} 