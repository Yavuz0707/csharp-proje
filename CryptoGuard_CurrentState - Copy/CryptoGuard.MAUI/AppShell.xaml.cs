using CryptoGuard.MAUI.Views;

namespace CryptoGuard.MAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));
		Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
		Routing.RegisterRoute(nameof(Views.MainPage), typeof(Views.MainPage));
		Routing.RegisterRoute(nameof(Views.ProfilePage), typeof(Views.ProfilePage));
		Routing.RegisterRoute("MyProfile", typeof(ProfilePage));
	}
}
