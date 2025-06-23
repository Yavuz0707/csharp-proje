using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class CommentPopupViewModel : ObservableObject
    {
        private readonly IFeedPostService _feedPostService;
        private readonly int _postId;

        [ObservableProperty]
        private ObservableCollection<Comment> comments = new();

        [ObservableProperty]
        private string newComment = string.Empty;

        public IRelayCommand SendCommentCommand { get; }
        public IRelayCommand<string> NavigateToUserProfileCommand { get; }

        public CommentPopupViewModel(IFeedPostService feedPostService, int postId)
        {
            _feedPostService = feedPostService;
            _postId = postId;
            SendCommentCommand = new AsyncRelayCommand(SendCommentAsync);
            NavigateToUserProfileCommand = new RelayCommand<string>(OnNavigateToUserProfile);
            _ = LoadCommentsAsync();
        }

        private async Task LoadCommentsAsync()
        {
            var list = await _feedPostService.GetCommentsAsync(_postId);
            Comments = new ObservableCollection<Comment>(list);
        }

        private async Task SendCommentAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewComment) && UserContext.CurrentUserId != null)
            {
                await _feedPostService.AddCommentAsync(_postId, UserContext.CurrentUserId.Value, NewComment);
                NewComment = string.Empty;
                await LoadCommentsAsync();
            }
        }

        private async void OnNavigateToUserProfile(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var route = $"ProfilePage?username={username}";
                await Shell.Current.GoToAsync(route);
            }
        }
    }
} 