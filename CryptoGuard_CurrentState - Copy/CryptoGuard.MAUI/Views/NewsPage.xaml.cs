using System;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Views
{
    public partial class NewsPage : ContentPage
    {
        public NewsPage()
        {
            InitializeComponent();
        }

        private async void OnGoToNewsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    await Launcher.Default.OpenAsync(url);
                }
                catch (Exception)
                {
                    await DisplayAlert("Hata", "Haber linki açılamadı.", "Tamam");
                }
            }
        }

        private async void OnNewsSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is CryptoGuard.Core.Models.NewsItem selectedNews)
            {
                var popup = new NewsDetailPopup(selectedNews);
                await Shell.Current.Navigation.PushModalAsync(popup);
                ((CollectionView)sender).SelectedItem = null;
            }
        }
    }
} 