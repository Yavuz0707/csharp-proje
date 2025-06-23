using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Maui.Storage;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace CryptoGuard.MAUI.ViewModels
{
    [QueryProperty(nameof(Username), "username")]
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly IFeedPostService _feedPostService;
        private readonly IPortfolioService _portfolioService;
        private readonly ICoinLoreService _coinLoreService;

        [ObservableProperty]
        private string userProfilePhoto = "profile_placeholder.png";

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private bool isPortfolioPublic = false;

        [ObservableProperty]
        private string portfolioProfit = "-";

        [ObservableProperty]
        private ObservableCollection<FeedPost> userPosts = new();

        [ObservableProperty]
        private ISeries[] pieSeries = Array.Empty<ISeries>();
        [ObservableProperty]
        private ISeries[] lineSeries = Array.Empty<ISeries>();
        [ObservableProperty]
        private decimal totalProfit;
        [ObservableProperty]
        private double totalProfitPercent;
        [ObservableProperty]
        private decimal totalPortfolioValue;
        [ObservableProperty]
        private double totalPortfolioChangePercent;
        [ObservableProperty]
        private int coinCount;

        [ObservableProperty]
        private string biography = "Henüz biyografi eklenmedi.";

        [ObservableProperty]
        private string biggestCoinName = "-";

        [ObservableProperty]
        private bool isFollowing;
        [ObservableProperty]
        private int followersCount;
        [ObservableProperty]
        private int followingCount;

        [ObservableProperty]
        private bool isPortfolioVisible = true;

        [ObservableProperty]
        private LiveChartsCore.SkiaSharpView.Axis[] mainChartAxes = new LiveChartsCore.SkiaSharpView.Axis[]
        {
            new LiveChartsCore.SkiaSharpView.Axis
            {
                Name = "Tutar (₺)",
                NamePaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColors.LightGray, 16),
                Labeler = value => value.ToString("N0"),
                TextSize = 14,
                MinLimit = 0,
                MaxLimit = 5000,
                MinStep = 100
            }
        };

        public IRelayCommand FollowCommand { get; }
        public IRelayCommand UnfollowCommand { get; }
        public IRelayCommand<FeedPost> LikeCommand { get; }
        public IRelayCommand<FeedPost> CommentCommand { get; }

        public ProfileViewModel(IUserService userService, IFeedPostService feedPostService, IPortfolioService portfolioService, ICoinLoreService coinLoreService)
        {
            _userService = userService;
            _feedPostService = feedPostService;
            _portfolioService = portfolioService;
            _coinLoreService = coinLoreService;
            FollowCommand = new RelayCommand(async () => await FollowUserAsync());
            UnfollowCommand = new RelayCommand(async () => await UnfollowUserAsync());
            LikeCommand = new RelayCommand<FeedPost>(async (post) => await LikePostAsync(post));
            CommentCommand = new RelayCommand<FeedPost>(async (post) => await CommentPostAsync(post));
        }

        partial void OnUsernameChanged(string value)
        {
            LoadUserProfile(value);
        }

        partial void OnPieSeriesChanged(ISeries[] value)
        {
            if (value != null && value.Length > 0)
            {
                var biggest = value.OfType<LiveChartsCore.SkiaSharpView.PieSeries<double>>()
                    .OrderByDescending(ps => ps.Values?.OfType<double>().FirstOrDefault() ?? 0)
                    .FirstOrDefault();
                BiggestCoinName = biggest?.Name ?? "-";
            }
            else
            {
                BiggestCoinName = "-";
            }
        }

        private async void LoadUserProfile(string username)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileViewModel.LoadUserProfile: username parametresi = {username}");
            // State temizliği (Username'i tekrar set etmeden!)
            Biography = "Henüz biyografi eklenmedi.";
            UserProfilePhoto = "profile_placeholder.png";
            IsPortfolioVisible = false;
            FollowersCount = 0;
            FollowingCount = 0;
            IsFollowing = false;
            TotalPortfolioValue = 0;
            CoinCount = 0;
            TotalProfit = 0;
            TotalProfitPercent = 0;
            PieSeries = Array.Empty<ISeries>();
            LineSeries = Array.Empty<ISeries>();
            UserPosts.Clear();
            // ... diğer state temizliği ...

            if (!string.IsNullOrEmpty(username))
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                System.Diagnostics.Debug.WriteLine($"ProfileViewModel.LoadUserProfile: user found = {(user != null ? user.Username : "null")}");
                if (user != null)
                {
                    if (user.Id != UserContext.CurrentUserId) return; // Sadece kendi profilini göster
                    Biography = user.Biography ?? "Henüz biyografi eklenmedi.";
                    UserProfilePhoto = user.ProfilePhoto ?? "profile_placeholder.png";
                    IsPortfolioVisible = user.IsPortfolioPublic;

                    // Portföy bilgilerini yükle
                    var items = await _portfolioService.GetPortfolioItemsByUserIdAsync(user.Id);
                    decimal totalBuy = 0;
                    decimal totalCurrent = 0;
                    foreach (var item in items)
                    {
                        decimal buyValue = item.Quantity * item.BuyPrice;
                        decimal currentValue = item.Quantity * (item.Coin?.CurrentPrice ?? 0);
                        totalBuy += buyValue;
                        totalCurrent += currentValue;
                    }
                    TotalProfit = totalCurrent - totalBuy;
                    TotalProfitPercent = totalBuy > 0 ? (double)(TotalProfit / totalBuy * 100M) : 0;

                    var pieList = new List<PieSeries<double>>();
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
                    double total = items.Sum(x => (double)(x.Quantity * (x.Coin?.CurrentPrice ?? 0)));
                    int colorIndex = 0;
                    foreach (var item in items)
                    {
                        var coinName = item.Coin?.Name ?? item.CoinId;
                        var coinSymbol = item.Coin?.Symbol ?? "";
                        decimal currentValue = item.Quantity * (item.Coin?.CurrentPrice ?? 0);
                        pieList.Add(new PieSeries<double>
                        {
                            Name = $"{coinName} ({coinSymbol})",
                            Values = new[] { (double)currentValue },
                            DataLabelsFormatter = point => $"{coinSymbol}: {(point.Model / total * 100):F2}%",
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                            DataLabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColors.White),
                            DataLabelsSize = 12,
                            Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(colors[colorIndex % colors.Length])
                        });
                        colorIndex++;
                    }
                    PieSeries = pieList.ToArray();
                    TotalPortfolioValue = (decimal)total;
                    CoinCount = items.Count();

                    // Grafik için küçük değerlerle örnek veri
                    var now = DateTime.Now;
                    var points = new List<LiveChartsCore.Defaults.DateTimePoint>();
                    double baseValue = 2000;
                    for (int i = 6; i >= 0; i--)
                    {
                        double value = baseValue + i * 50;
                        points.Add(new LiveChartsCore.Defaults.DateTimePoint(now.AddDays(-i), value));
                    }
                    LineSeries = new ISeries[]
                    {
                        new LiveChartsCore.SkiaSharpView.LineSeries<LiveChartsCore.Defaults.DateTimePoint>
                        {
                            Name = "Portföy Değeri",
                            Values = points,
                            Stroke = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColors.DodgerBlue, 2),
                            GeometrySize = 8,
                            Fill = null
                        }
                    };
                    MainChartAxes = new LiveChartsCore.SkiaSharpView.Axis[]
                    {
                        new LiveChartsCore.SkiaSharpView.Axis
                        {
                            Name = "Tutar (₺)",
                            NamePaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColors.LightGray, 16),
                            Labeler = value => value.ToString("N0"),
                            TextSize = 14,
                            MinLimit = 0,
                            MaxLimit = 5000,
                            MinStep = 100
                        }
                    };

                    // Kullanıcının gönderilerini çek ve UserPosts'a ekle
                    var posts = await _feedPostService.GetPostsByUserIdAsync(user.Id);
                    UserPosts.Clear();
                    foreach (var post in posts)
                        UserPosts.Add(post);
                }
            }
        }

        // FeedPostsSource: Tüm gönderilerin merkezi listesi (örnek)
        public static List<FeedPost> FeedPostsSource = new List<FeedPost>
        {
            new FeedPost { User = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "dummy" }, Content = "BTC için yükseliş bekliyorum!", CoinTag = "BTC", CreatedAt = System.DateTime.Now },
            new FeedPost { User = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "dummy" }, Content = "ETH portföyümde tutuyorum.", CoinTag = "ETH", CreatedAt = System.DateTime.Now },
            new FeedPost { User = new User { Username = "bob", Email = "bob@example.com", PasswordHash = "dummy" }, Content = "SOL için analizim var!", CoinTag = "SOL", CreatedAt = System.DateTime.Now }
        };

        public bool IsMyPost(FeedPost post) => UserContext.CurrentUserId != null && post.UserId == UserContext.CurrentUserId;

        public ICommand DeletePostCommand => new Command<FeedPost>(async (post) => await DeletePostAsync(post));
        public ICommand EditPostCommand => new Command<FeedPost>(async (post) => await EditPostAsync(post));

        private async Task DeletePostAsync(FeedPost post)
        {
            if (post == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Gönderi Sil", "Bu gönderiyi silmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (!confirm) return;
            await _feedPostService.DeletePostAsync(post.Id);
            UserPosts.Remove(post);
        }

        private async Task EditPostAsync(FeedPost post)
        {
            if (post == null) return;
            string newContent = await Shell.Current.DisplayPromptAsync("Gönderiyi Düzenle", "Yeni içeriği girin:", initialValue: post.Content);
            if (string.IsNullOrWhiteSpace(newContent)) return;
            post.Content = newContent;
            await _feedPostService.UpdatePostAsync(post);
            // Listeyi yenile
            var posts = await _feedPostService.GetPostsByUserIdAsync(post.UserId);
            UserPosts.Clear();
            foreach (var p in posts)
                UserPosts.Add(p);
        }

        private async Task FollowUserAsync()
        {
            if (UserContext.CurrentUserId == null) return;
            var user = await _userService.GetUserByUsernameAsync(Username);
            if (user == null) return;
            var result = await _userService.FollowUserAsync(UserContext.CurrentUserId.Value, user.Id);
            if (result)
            {
                IsFollowing = true;
                FollowersCount++;
            }
        }

        private async Task UnfollowUserAsync()
        {
            if (UserContext.CurrentUserId == null) return;
            var user = await _userService.GetUserByUsernameAsync(Username);
            if (user == null) return;
            var result = await _userService.UnfollowUserAsync(UserContext.CurrentUserId.Value, user.Id);
            if (result)
            {
                IsFollowing = false;
                FollowersCount = Math.Max(0, FollowersCount - 1);
            }
        }

        private async Task LikePostAsync(FeedPost post)
        {
            System.Diagnostics.Debug.WriteLine($"LikePostAsync çağrıldı: {post?.Content}");
            if (post == null || UserContext.CurrentUserId == null) return;
            await _feedPostService.LikePostAsync(post.Id, UserContext.CurrentUserId.Value);
            // Beğeni sayısını güncelle
            post.LikeCount++;
            // UI'yi yenilemek için koleksiyonu tetikle
            var idx = UserPosts.IndexOf(post);
            if (idx >= 0)
            {
                UserPosts.RemoveAt(idx);
                UserPosts.Insert(idx, post);
            }
        }

        private async Task CommentPostAsync(FeedPost post)
        {
            System.Diagnostics.Debug.WriteLine($"CommentPostAsync çağrıldı: {post?.Content}");
            if (post == null || UserContext.CurrentUserId == null) return;
            string commentText = await Shell.Current.DisplayPromptAsync("Yorum Ekle", "Yorumunuzu yazın:");
            if (string.IsNullOrWhiteSpace(commentText)) return;
            await _feedPostService.AddCommentAsync(post.Id, UserContext.CurrentUserId.Value, commentText);
            // Yorum sayısını güncelle
            post.CommentCount++;
            var idx = UserPosts.IndexOf(post);
            if (idx >= 0)
            {
                UserPosts.RemoveAt(idx);
                UserPosts.Insert(idx, post);
            }
        }
    }
} 