using CryptoGuard.MAUI.ViewModels;
using CryptoGuard.Core.Interfaces;

namespace CryptoGuard.MAUI.Views
{
    [QueryProperty(nameof(Hashtag), "hashtag")]
    public partial class HashtagPostsPage : ContentPage
    {
        private readonly IFeedPostService _feedPostService;
        private string _hashtag;
        public string Hashtag
        {
            get => _hashtag;
            set
            {
                _hashtag = value;
                BindingContext = new HashtagPostsViewModel(_feedPostService, _hashtag);
            }
        }
        public HashtagPostsPage(IFeedPostService feedPostService)
        {
            InitializeComponent();
            _feedPostService = feedPostService;
        }
    }
} 