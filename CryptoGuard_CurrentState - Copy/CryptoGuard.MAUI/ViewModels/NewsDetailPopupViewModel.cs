using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoGuard.Core.Models;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class NewsDetailPopupViewModel : ObservableObject
    {
        public NewsItem News { get; }
        public string Title => News.Title;
        public string Description => News.Description;
        public string ImgUrl => News.ImgUrl;
        public string Source => News.Source;
        public string PubDate => News.PubDate;
        public string FormattedPubDate
        {
            get
            {
                if (DateTime.TryParse(PubDate, out var dt))
                    return dt.ToString("dd.MM.yyyy");
                return PubDate;
            }
        }
        public ICommand GoToNewsCommand { get; }
        public ICommand CloseCommand { get; }

        public NewsDetailPopupViewModel(NewsItem news)
        {
            News = news;
            GoToNewsCommand = new RelayCommand(OpenNews);
            CloseCommand = new RelayCommand(Close);
        }

        private async void OpenNews()
        {
            if (!string.IsNullOrEmpty(News.Link))
                await Launcher.Default.OpenAsync(News.Link);
        }

        private async void Close()
        {
            await Shell.Current.Navigation.PopModalAsync();
        }
    }
} 