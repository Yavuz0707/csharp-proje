using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Views
{
    public partial class ImagePopup : CommunityToolkit.Maui.Views.Popup
    {
        public Command CloseCommand { get; }
        public ImagePopup(string imagePath)
        {
            InitializeComponent();
            PopupImage.Source = imagePath;
            CloseCommand = new Command(() => Close());
            BindingContext = this;
        }
    }
} 