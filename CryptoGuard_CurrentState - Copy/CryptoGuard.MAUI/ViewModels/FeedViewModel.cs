using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using CommunityToolkit.Maui.Views;
using CryptoGuard.MAUI.Views;
using CommunityToolkit.Maui.Core;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class FeedViewModel : ObservableObject
    {
        private readonly IFeedPostService _feedPostService;
        private readonly IUserService _userService;

        [ObservableProperty]
        private ObservableCollection<FeedPost> feedPosts = new();

        [ObservableProperty]
        private string newPostContent;

        [ObservableProperty]
        private string newPostImagePath;

        [ObservableProperty]
        private string currentUserProfilePhoto = "profile_placeholder.png";

        [ObservableProperty]
        private string userSearchText;

        [ObservableProperty]
        private ObservableCollection<User> userSearchResults = new();

        public ICommand SharePostCommand { get; }
        public ICommand LikeCommand { get; }
        public ICommand CommentCommand { get; }
        public ICommand QuoteCommand { get; }
        public ICommand RetweetCommand { get; }
        public IRelayCommand SearchUsersCommand { get; }
        public IRelayCommand<string> OpenUserProfileCommand { get; }
        public IRelayCommand<string> OpenProfileCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand ShowEmojiPickerCommand { get; }
        public ICommand FeedPostMenuCommand { get; }
        public ICommand ShowImagePopupCommand { get; }
        public ICommand LoadFeedCommand { get; }
        public IRelayCommand<int> OpenOriginalPostCommand { get; }

        public FeedViewModel(IFeedPostService feedPostService, IUserService userService)
        {
            _feedPostService = feedPostService;
            _userService = userService;
            SharePostCommand = new AsyncRelayCommand(SharePostAsync);
            LikeCommand = new RelayCommand<FeedPost>(OnLike);
            CommentCommand = new RelayCommand<FeedPost>(OnComment);
            QuoteCommand = new RelayCommand<FeedPost>(OnQuote);
            RetweetCommand = new AsyncRelayCommand<FeedPost>(RetweetAsync);
            OpenProfileCommand = new RelayCommand<string>(OnOpenProfile);
            SearchUsersCommand = new RelayCommand(async () => await SearchUsersAsync());
            OpenUserProfileCommand = new RelayCommand<string>(OnOpenUserProfile);
            SelectImageCommand = new AsyncRelayCommand(SelectImageAsync);
            ShowEmojiPickerCommand = new AsyncRelayCommand(ShowEmojiPickerAsync);
            FeedPostMenuCommand = new AsyncRelayCommand<FeedPost>(OnFeedPostMenuClicked);
            ShowImagePopupCommand = new RelayCommand<string>(OnShowImagePopup);
            LoadFeedCommand = new AsyncRelayCommand(LoadFeedAsync);
            OpenOriginalPostCommand = new AsyncRelayCommand<int>(OpenOriginalPostAsync);
            LoadFeedAsync();
            MessagingCenter.Subscribe<QuotePopupViewModel>(this, "RefreshFeed", async (sender) => await LoadFeedAsync());
        }

        public async Task LoadFeedAsync()
        {
            System.Diagnostics.Debug.WriteLine("LoadFeedAsync çağrıldı");
            var posts = await _feedPostService.GetAllPostsAsync();
            System.Diagnostics.Debug.WriteLine($"Post count: {posts.Count}");
            foreach (var post in posts)
            {
                System.Diagnostics.Debug.WriteLine($"PostId: {post.Id}, UserId: {post.UserId}, Username: {post.User?.Username}");
                post.IsMine = UserContext.CurrentUserId != null && post.UserId == UserContext.CurrentUserId;
                if (UserContext.CurrentUserId != null)
                {
                    var likes = await _feedPostService.GetLikesAsync(post.Id);
                    post.IsLikedByMe = likes.Any(l => l.UserId == UserContext.CurrentUserId);
                }
                else
                {
                    post.IsLikedByMe = false;
                }
            }
            FeedPosts = new ObservableCollection<FeedPost>(posts);
        }

        private async Task SharePostAsync()
        {
            try
            {
                if (UserContext.CurrentUserId == null)
                {
                    await Shell.Current.DisplayAlert("Hata", "Oturum açmadan gönderi paylaşamazsınız.", "Tamam");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(NewPostContent) || !string.IsNullOrWhiteSpace(NewPostImagePath))
                {
                    var user = await _userService.GetUserByIdAsync(UserContext.CurrentUserId.Value);
                    if (user == null)
                    {
                        await Shell.Current.DisplayAlert("Hata", "Kullanıcı bulunamadı.", "Tamam");
                        return;
                    }
                    var post = new FeedPost
                    {
                        UserId = user.Id,
                        Content = NewPostContent,
                        CoinTag = "",
                        CreatedAt = DateTime.Now,
                        ImagePath = string.IsNullOrEmpty(NewPostImagePath) ? "" : NewPostImagePath
                    };
                    await _feedPostService.AddPostAsync(post);
                    MessagingCenter.Send(this, "RefreshFeed");
                    NewPostContent = string.Empty;
                    NewPostImagePath = string.Empty;
                    await LoadFeedAsync();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", ex.Message, "Tamam");
            }
        }

        private async void OnLike(FeedPost post)
        {
            await ToggleLikeAsync(post);
        }
        private async void OnComment(FeedPost post)
        {
            var popupVm = new CommentPopupViewModel(_feedPostService, post.Id);
            var popup = new CryptoGuard.MAUI.Views.CommentPopup(popupVm);
            await Application.Current.MainPage.Navigation.PushModalAsync(popup);
        }
        private async void OnQuote(FeedPost post)
        {
            var popupVm = new QuotePopupViewModel(_feedPostService, _userService, post.Id);
            var popup = new QuotePopup(popupVm);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }
        private async void OnOpenProfile(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var route = $"ProfilePage?username={username}";
                await Shell.Current.GoToAsync(route);
            }
        }

        public async Task DeletePostFromFeedAsync(int postId)
        {
            var post = FeedPosts.FirstOrDefault(p => p.Id == postId);
            if (post == null || !post.IsMine)
            {
                await Shell.Current.DisplayAlert("Uyarı", "Bu gönderi size ait değil.", "Tamam");
                return;
            }
            await _feedPostService.DeletePostAsync(postId);
            await LoadFeedAsync();
        }

        public async Task EditPostFromFeedAsync(FeedPost post, string newContent)
        {
            if (post == null || !post.IsMine)
            {
                await Shell.Current.DisplayAlert("Uyarı", "Bu gönderi size ait değil.", "Tamam");
                return;
            }
            post.Content = newContent;
            await _feedPostService.UpdatePostAsync(post);
            await LoadFeedAsync();
        }

        public async Task ToggleLikeAsync(FeedPost post)
        {
            if (UserContext.CurrentUserId == null) return;
            var userId = UserContext.CurrentUserId.Value;
            var likes = await _feedPostService.GetLikesAsync(post.Id);
            var alreadyLiked = likes.Any(l => l.UserId == userId);
            if (alreadyLiked)
                await _feedPostService.UnlikePostAsync(post.Id, userId);
            else
                await _feedPostService.LikePostAsync(post.Id, userId);
            await RefreshPostAsync(post);
        }

        public async Task AddCommentAsync(FeedPost post, string content)
        {
            if (UserContext.CurrentUserId == null) return;
            var userId = UserContext.CurrentUserId.Value;
            await _feedPostService.AddCommentAsync(post.Id, userId, content);
            await RefreshPostAsync(post);
        }

        public async Task RefreshPostAsync(FeedPost post)
        {
            // Postu güncel verilerle yenile
            var updated = (await _feedPostService.GetAllPostsAsync()).FirstOrDefault(p => p.Id == post.Id);
            if (updated != null)
            {
                if (UserContext.CurrentUserId != null)
                {
                    var likes = await _feedPostService.GetLikesAsync(post.Id);
                    updated.IsLikedByMe = likes.Any(l => l.UserId == UserContext.CurrentUserId);
                }
                else
                {
                    updated.IsLikedByMe = false;
                }
                var idx = FeedPosts.IndexOf(post);
                if (idx >= 0)
                {
                    FeedPosts[idx] = updated;
                }
            }
        }

        public async Task<bool> IsPostLikedByCurrentUserAsync(FeedPost post)
        {
            if (UserContext.CurrentUserId == null) return false;
            var likes = await _feedPostService.GetLikesAsync(post.Id);
            return likes.Any(l => l.UserId == UserContext.CurrentUserId);
        }

        public async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(UserSearchText))
            {
                UserSearchResults = new ObservableCollection<User>();
                return;
            }
            var users = await _userService.GetAllUsersAsync();
            var filtered = users.Where(u => u.Username.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            UserSearchResults = new ObservableCollection<User>(filtered);
        }

        private async void OnOpenUserProfile(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var route = $"ProfilePage?username={username}";
                await Shell.Current.GoToAsync(route);
            }
        }

        private async Task SelectImageAsync()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Resim seç",
                    FileTypes = FilePickerFileType.Images
                });
                if (result != null)
                {
                    // Seçilen resmi uygulama dizinine kopyala
                    var fileName = Path.GetFileName(result.FullPath);
                    var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    using (var sourceStream = await result.OpenReadAsync())
                    using (var destStream = File.OpenWrite(destPath))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                    NewPostImagePath = destPath;
                }
            }
            catch { }
        }

        private async Task ShowEmojiPickerAsync()
        {
            var emojis = new[] { "😀", "😂", "😍", "🔥", "👍", "🎉", "😢", "😎", "🥳", "🙏" };
            var selected = await Shell.Current.DisplayActionSheet("Emoji seç", "İptal", null, emojis);
            if (!string.IsNullOrEmpty(selected) && selected != "İptal")
            {
                NewPostContent += selected;
            }
        }

        private async Task RetweetAsync(FeedPost post)
        {
            if (post == null) return;
            string action = await Shell.Current.DisplayActionSheet("Retweet", "İptal", null, "Repost", "Quote");
            if (action == "Repost")
            {
                if (UserContext.CurrentUserId == null) return;
                var username = UserContext.CurrentUsername ?? "Bilinmeyen";
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null) return;
                var retweet = new FeedPost
                {
                    UserId = user.Id,
                    Content = post.Content,
                    CoinTag = post.CoinTag,
                    CreatedAt = DateTime.Now,
                    ImagePath = string.IsNullOrEmpty(post.ImagePath) ? "" : post.ImagePath,
                    OriginalPostId = post.Id
                };
                await _feedPostService.AddPostAsync(retweet);
                await LoadFeedAsync();
            }
            else if (action == "Quote")
            {
                var popupVm = new QuotePopupViewModel(_feedPostService, _userService, post.Id);
                var popup = new QuotePopup(popupVm);
                await Shell.Current.CurrentPage.ShowPopupAsync(popup);
            }
        }

        private async Task OnFeedPostMenuClicked(FeedPost post)
        {
            if (post == null) return;
            string action = await Shell.Current.DisplayActionSheet("Gönderi İşlemleri", "İptal", null, "Düzenle", "Sil");
            if (action == "Sil")
            {
                await DeletePostFromFeedAsync(post.Id);
            }
            else if (action == "Düzenle")
            {
                string newContent = await Shell.Current.DisplayPromptAsync("Gönderiyi Düzenle", "Yeni içeriği girin:", initialValue: post.Content);
                if (!string.IsNullOrWhiteSpace(newContent))
                {
                    await EditPostFromFeedAsync(post, newContent);
                }
            }
        }

        private async void OnShowImagePopup(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;
            var popup = new ImagePopup(imagePath);
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }

        private async Task OpenOriginalPostAsync(int postId)
        {
            MessagingCenter.Send(this, "ScrollToPost", postId);
        }
    }
} 