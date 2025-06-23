using CommunityToolkit.Maui.Views;

namespace CryptoGuard.MAUI.Views;

public partial class AIPopup : Popup
{
    public AIPopup(string title, string answer)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        AnswerLabel.Text = answer;
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
} 