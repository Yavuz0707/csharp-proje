using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class HashtagPostsViewModel : ObservableObject
    {
        private readonly IFeedPostService _feedPostService;
        [ObservableProperty]
        private string hashtag;
        [ObservableProperty]
        private ObservableCollection<FeedPost> posts = new();

        public HashtagPostsViewModel(IFeedPostService feedPostService, string hashtag)
        {
            _feedPostService = feedPostService;
            Hashtag = hashtag;
            LoadPosts();
        }

        private async void LoadPosts()
        {
            var allPosts = await _feedPostService.GetAllPostsAsync();
            var filtered = allPosts.Where(p => !string.IsNullOrWhiteSpace(p.Content) && p.Content.Contains(Hashtag)).ToList();
            Posts = new ObservableCollection<FeedPost>(filtered);
        }
    }
} 