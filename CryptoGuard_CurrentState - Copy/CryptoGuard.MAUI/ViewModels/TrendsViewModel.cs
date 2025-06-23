using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.ViewModels
{
    public class TrendingHashtag
    {
        public string Hashtag { get; set; }
        public int Count { get; set; }
    }

    public partial class TrendsViewModel : ObservableObject
    {
        private readonly IFeedPostService _feedPostService;
        public ObservableCollection<TrendingHashtag> TrendingHashtags { get; set; } = new();
        public ICommand HashtagSelectedCommand { get; }

        public TrendsViewModel(IFeedPostService feedPostService)
        {
            _feedPostService = feedPostService;
            HashtagSelectedCommand = new RelayCommand<string>(OnHashtagSelected);
            LoadTrendingHashtags();
        }

        private async void LoadTrendingHashtags()
        {
            var dict = await _feedPostService.GetTrendingHashtagsAsync(10);
            TrendingHashtags.Clear();
            foreach (var kv in dict)
                TrendingHashtags.Add(new TrendingHashtag { Hashtag = kv.Key, Count = kv.Value });
        }

        private async void OnHashtagSelected(string hashtag)
        {
            if (!string.IsNullOrWhiteSpace(hashtag))
            {
                var route = $"HashtagPostsPage?hashtag={hashtag}";
                await Shell.Current.GoToAsync(route);
            }
        }
    }
} 