using Microsoft.Maui.Controls;
using CryptoGuard.Core.Models;
using CryptoGuard.MAUI.ViewModels;
using System;
using System.Linq;

namespace CryptoGuard.MAUI.Views
{
    public partial class FeedPage : ContentPage
    {
        public FeedPage(FeedViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            MessagingCenter.Subscribe<FeedViewModel, int>(this, "ScrollToPost", (sender, postId) =>
            {
                ScrollToPostId(postId);
            });
        }

        public FeedPage() : this(App.ServiceProvider.GetRequiredService<FeedViewModel>()) { }

        private async void OnFeedPostMenuClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton btn && btn.CommandParameter is FeedPost post && BindingContext is FeedViewModel vm)
            {
                string action = await DisplayActionSheet("Gönderi İşlemleri", "İptal", null, "Düzenle", "Sil");
                if (action == "Sil")
                {
                    await vm.DeletePostFromFeedAsync(post.Id);
                }
                else if (action == "Düzenle")
                {
                    string newContent = await DisplayPromptAsync("Gönderiyi Düzenle", "Yeni içeriği girin:", initialValue: post.Content);
                    if (!string.IsNullOrWhiteSpace(newContent))
                    {
                        await vm.EditPostFromFeedAsync(post, newContent);
                    }
                }
            }
        }

        private async void OnUserSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is FeedViewModel vm)
            {
                await vm.SearchUsersAsync();
            }
        }

        public void ScrollToPostId(int postId)
        {
            if (BindingContext is FeedViewModel vm)
            {
                var post = vm.FeedPosts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    var index = vm.FeedPosts.IndexOf(post);
                    if (index >= 0)
                        feedCollectionView.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
                }
            }
        }
    }
} 