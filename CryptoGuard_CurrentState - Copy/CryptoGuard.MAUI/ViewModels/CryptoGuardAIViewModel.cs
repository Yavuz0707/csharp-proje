using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class CryptoGuardAIViewModel : BaseViewModel
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ICoinLoreService _coinLoreService;

        [ObservableProperty]
        private string customQuestion = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private ObservableCollection<string> chatHistory = new();

        [ObservableProperty]
        private ObservableCollection<string> exampleCommands = new();

        [ObservableProperty]
        private bool isExampleCommandsVisible = false;

        [ObservableProperty]
        private string userMessage;

        private readonly string cohereApiKey = "V20apE0rnbJLoMBA26bbaNXxxbo33gxsVAKhxJtZ";
        private readonly string cohereModel = "command";

        public CryptoGuardAIViewModel(IPortfolioService portfolioService, ICoinLoreService coinLoreService)
        {
            _portfolioService = portfolioService;
            _coinLoreService = coinLoreService;
            
            // Örnek komutları ekle
            ExampleCommands.Add("Portföyümde en riskli coin hangisi?");
            ExampleCommands.Add("En çok kazandıran coinim hangisi?");
            ExampleCommands.Add("Portföyümde çeşitlendirme yapmalı mıyım?");
            ExampleCommands.Add("Satmalı mıyım?");
            ExampleCommands.Add("Portföyümde toplam kaç coin var?");
            ExampleCommands.Add("Portföyümde en çok ağırlığı olan coin nedir?");
            ExampleCommands.Add("Son bir ayda en çok değer kaybeden coinim hangisi?");
            ExampleCommands.Add("Bana portföyümle ilgili öneri verir misin?");
            ExampleCommands.Add("Portföyümdeki coinlerin performansını özetler misin?");
            ExampleCommands.Add("Riskimi azaltmak için ne yapmalıyım?");
        }

        [RelayCommand]
        private void ToggleExampleCommands()
        {
            IsExampleCommandsVisible = !IsExampleCommandsVisible;
        }

        [RelayCommand]
        private async Task AnalyzePortfolio()
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

                var items = await _portfolioService.GetPortfolioItemsByUserIdAsync(UserContext.CurrentUserId.Value);
                var analysis = await GetRealAICryptoAnswer("Portföyümün performansını analiz et ve önerilerde bulun");
                await Shell.Current.DisplayAlert("Portföy Analizi", analysis, "Tamam");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Analiz yapılırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GetPortfolioRecommendations()
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

                var items = await _portfolioService.GetPortfolioItemsByUserIdAsync(UserContext.CurrentUserId.Value);
                var recommendations = await GetRealAICryptoAnswer("Portföyüm için önerilerde bulun");
                await Shell.Current.DisplayAlert("Portföy Önerileri", recommendations, "Tamam");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Öneriler alınırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AskAIFuturePrice(string coinId)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var question = $"{coinId.ToUpper()}'nin gelecekteki fiyatını ne etkileyebilir?";
                var answer = await GetRealAICryptoAnswer(question);
                await Shell.Current.DisplayAlert($"{coinId.ToUpper()} Analizi", answer, "Tamam");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Analiz yapılırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AskAIMarketAnalysis()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var answer = await GetRealAICryptoAnswer("Kripto piyasasının genel durumu nedir?");
                await Shell.Current.DisplayAlert("Piyasa Analizi", answer, "Tamam");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Analiz yapılırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AskAITopPerformers()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var answer = await GetRealAICryptoAnswer("En iyi performans gösteren coinler hangileri?");
                await Shell.Current.DisplayAlert("En İyi Performans Gösteren Coinler", answer, "Tamam");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Analiz yapılırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AskCustomQuestion()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(CustomQuestion)) return;
            IsBusy = true;
            try
            {
                var answer = await GetRealAICryptoAnswer(CustomQuestion);
                await Shell.Current.DisplayAlert("AI Yanıtı", answer, "Tamam");
                CustomQuestion = string.Empty;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", $"Soru yanıtlanırken hata oluştu: {ex.Message}", "Tamam");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<string> GetRealAICryptoAnswer(string question)
        {
            // Burada gerçek bir AI servisi entegrasyonu yapılabilir
            // Şimdilik örnek yanıtlar döndürüyoruz
            await Task.Delay(1000); // Simüle edilmiş API çağrısı

            return question.ToLower() switch
            {
                var q when q.Contains("performans") => "Portföyünüzün performansı son 30 günde %15 artış gösterdi. Bitcoin ve Ethereum pozisyonlarınız en iyi performansı gösteriyor. Diversifikasyon için DeFi ve NFT projelerini değerlendirebilirsiniz.",
                var q when q.Contains("öneri") => "Portföyünüzü çeşitlendirmek için Layer 2 çözümleri ve DeFi protokolleri ekleyebilirsiniz. Ayrıca, risk yönetimi için stop-loss seviyeleri belirlemenizi öneririm.",
                var q when q.Contains("bitcoin") => "Bitcoin'in gelecekteki fiyatını etkileyebilecek faktörler: 1) Kurumsal yatırımcıların BTC ETF'lerine ilgisi 2) Küresel ekonomik koşullar 3) Regülasyon gelişmeleri 4) Halving etkisi",
                var q when q.Contains("ethereum") => "Ethereum'un gelecekteki fiyatını etkileyebilecek faktörler: 1) Layer 2 çözümlerinin gelişimi 2) DeFi ve NFT ekosisteminin büyümesi 3) Proof of Stake geçişinin etkileri 4) EIP güncellemeleri",
                var q when q.Contains("piyasa") => "Kripto piyasası şu anda yükseliş trendinde. Bitcoin'in 50,000$ seviyesini test etmesi ve Ethereum'un 3,000$ üzerinde kalması olumlu sinyaller veriyor. Ancak volatilite yüksek, risk yönetimi önemli.",
                var q when q.Contains("performans gösteren") => "Son 30 günde en iyi performans gösteren coinler: 1) Solana (SOL) +45% 2) Avalanche (AVAX) +35% 3) Polygon (MATIC) +30% 4) Cardano (ADA) +25% 5) Polkadot (DOT) +20%",
                _ => "Üzgünüm, bu soruya yanıt veremiyorum. Lütfen kripto para ve portföy yönetimi ile ilgili sorular sorun."
            };
        }

        [RelayCommand]
        private void ExampleCommandTapped(string command)
        {
            UserMessage = command;
            SendMessageCommand.Execute(null);
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(UserMessage)) return;
            ChatHistory.Add($"Siz: {UserMessage}");
            string reply = await GenerateReply(UserMessage);
            ChatHistory.Add($"AI: {reply}");
            UserMessage = string.Empty;
        }

        private async Task<string> GenerateReply(string message)
        {
            // 1. Portföy özetini hazırla
            var items = await _portfolioService.GetPortfolioItemsByUserIdAsync(UserContext.CurrentUserId.Value);
            string portfolioSummary = "";
            if (items != null && items.Count() > 0)
            {
                portfolioSummary = "Portföy Özeti:\n" + string.Join("\n", items.Select(x => $"{x.CoinName} ({x.CoinSymbol}): Miktar={x.Quantity}, K/Z={x.ProfitPercent:F2}%"));
            }
            else
            {
                portfolioSummary = "Portföyünüzde hiç coin yok.";
            }

            // 2. Cohere prompt'unu hazırla
            string prompt = $"Kullanıcı portföyü:\n{portfolioSummary}\n\nSoru: {message}\nKısa, net, yatırım tavsiyesi vermeden cevapla.";

            // 3. Cohere API'ya gönder
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {cohereApiKey}");
                client.DefaultRequestHeaders.Add("Cohere-Version", "2022-12-06");
                var requestBody = new
                {
                    model = cohereModel,
                    prompt = prompt,
                    max_tokens = 200,
                    temperature = 0.7,
                    stop_sequences = new string[] { "\n" }
                };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.cohere.ai/v1/generate", content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"Cohere API hatası: {error}";
                }
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var reply = doc.RootElement.GetProperty("generations")[0].GetProperty("text").GetString();
                return reply?.Trim() ?? "Cevap alınamadı.";
            }
            catch (System.Exception ex)
            {
                return $"Cohere API hatası: {ex.Message}";
            }
        }

        public async Task<string> SendMessageFromVoiceAsync(string message)
        {
            // Chat geçmişine eklemeden sadece cevap döndür
            return await GenerateReply(message);
        }
    }
} 