using CryptoGuard.Core.Models;
using CryptoGuard.MAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Views
{
    public partial class NewsDetailPopup : ContentPage
    {
        public NewsDetailPopup(NewsItem news)
        {
            InitializeComponent();
            BindingContext = new NewsDetailPopupViewModel(news);
        }
    }
} 