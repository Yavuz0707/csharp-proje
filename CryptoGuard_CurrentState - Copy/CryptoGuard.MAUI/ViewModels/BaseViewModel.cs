using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoGuard.MAUI.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        private string? title;
        public string? Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
    }
} 