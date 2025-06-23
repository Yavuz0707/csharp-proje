using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class QuotePopupViewModel : ObservableObject
    {
        private readonly IFeedPostService _feedPostService;
        private readonly IUserService _userService;
        private readonly int _originalPostId;
        [ObservableProperty]
        private string quoteText;
        [ObservableProperty]
        private FeedPost alintilananPost;
        [ObservableProperty]
        private string currentUserProfilePhoto = "profile_placeholder.png";
        [ObservableProperty]
        private string selectedImagePath;
        public ICommand ShareQuoteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectImageCommand { get; }
        public ICommand ShowEmojiPickerCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public event Action? CloseRequested;
        public QuotePopupViewModel(IFeedPostService feedPostService, IUserService userService, int originalPostId)
        {
            _feedPostService = feedPostService;
            _userService = userService;
            _originalPostId = originalPostId;
            ShareQuoteCommand = new AsyncRelayCommand(ShareQuoteAsync, CanShareQuote);
            CancelCommand = new AsyncRelayCommand(CloseAsync);
            SelectImageCommand = new AsyncRelayCommand(SelectImageAsync);
            ShowEmojiPickerCommand = new AsyncRelayCommand(ShowEmojiPickerAsync);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            SetCurrentUserProfilePhoto();
            _ = LoadAlintilananPostAsync();
        }
        private async void SetCurrentUserProfilePhoto()
        {
            if (UserContext.CurrentUserId != null)
            {
                var user = await _userService.GetUserByIdAsync(UserContext.CurrentUserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.ProfilePhoto))
                    CurrentUserProfilePhoto = user.ProfilePhoto;
                else
                    CurrentUserProfilePhoto = "profile_placeholder.png";
            }
            else
            {
                CurrentUserProfilePhoto = "profile_placeholder.png";
            }
        }
        private async Task LoadAlintilananPostAsync()
        {
            var posts = await _feedPostService.GetAllPostsAsync();
            AlintilananPost = posts.FirstOrDefault(p => p.Id == _originalPostId);
        }
        private async Task SelectImageAsync()
        {
            try
            {
                FileResult result = null;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    result = await FilePicker.PickAsync(new PickOptions
                    {
                        PickerTitle = "Resim seç",
                        FileTypes = FilePickerFileType.Images
                    });
                });
                if (result != null)
                {
                    var fileName = Path.GetFileName(result.FullPath);
                    var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                    using (var sourceStream = await result.OpenReadAsync())
                    using (var destStream = File.OpenWrite(destPath))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                    SelectedImagePath = destPath;
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
                QuoteText += selected;
            }
        }
        private async Task ShareQuoteAsync()
        {
            if (UserContext.CurrentUserId == null) return;
            var userId = UserContext.CurrentUserId.Value;
            var post = new FeedPost
            {
                UserId = userId,
                Content = QuoteText,
                CreatedAt = DateTime.Now,
                OriginalPostId = _originalPostId,
                CoinTag = "",
                ImagePath = SelectedImagePath
            };
            await _feedPostService.AddPostAsync(post);
            CloseRequested?.Invoke();
        }
        private async Task CloseAsync()
        {
            CloseRequested?.Invoke();
        }
        private void RemoveImage()
        {
            SelectedImagePath = null;
        }
        private bool CanShareQuote()
        {
            return !string.IsNullOrWhiteSpace(QuoteText) || !string.IsNullOrWhiteSpace(SelectedImagePath);
        }
        partial void OnQuoteTextChanged(string value)
        {
            ((AsyncRelayCommand)ShareQuoteCommand).NotifyCanExecuteChanged();
        }
        partial void OnSelectedImagePathChanged(string value)
        {
            ((AsyncRelayCommand)ShareQuoteCommand).NotifyCanExecuteChanged();
        }
    }
} 