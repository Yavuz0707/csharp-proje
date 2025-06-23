namespace CryptoGuard.MAUI;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using System.Diagnostics;
using System.Threading.Tasks;
using CryptoGuard.MAUI.Views;
using CryptoGuard.MAUI.ViewModels;
using CryptoGuard.Core.Models;
using Microsoft.Maui.Storage;

public partial class App : Application
{
	public static IServiceProvider ServiceProvider { get; set; }
	private static bool _isInitialized;

	public App(IServiceProvider serviceProvider)
	{
		try
		{
			Debug.WriteLine("App ctor başladı");
			InitializeComponent();
			Debug.WriteLine("App ctor bitti");
			ServiceProvider = serviceProvider;

			// Kullanıcı bilgilerini Preferences'tan yükle
			if (Preferences.ContainsKey("CurrentUserId"))
			{
				UserContext.CurrentUserId = Preferences.Get("CurrentUserId", 0);
			}
			if (Preferences.ContainsKey("CurrentUsername"))
			{
				UserContext.CurrentUsername = Preferences.Get("CurrentUsername", "");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("App ctor exception: " + ex);
			MainPage = new ContentPage { Content = new Label { Text = "App Error: " + ex.ToString() } };
		}
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			Debug.WriteLine("Global Unhandled Exception: " + ex);
			MainPage = new ContentPage { Content = new Label { Text = "Global Error: " + ex?.ToString() } };
		};
	}

	private void InitializeLiveCharts()
	{
		if (!_isInitialized)
		{
			LiveCharts.Configure(config => config
				.AddSkiaSharp()
				.AddDefaultMappers()
				.AddLightTheme()
			);
			_isInitialized = true;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			var shell = new AppShell();
			var window = new Window(shell);

			// Asenkron işlemleri ayrı bir task'ta yap
			Task.Run(async () =>
			{
				await Task.Delay(100); // Kısa bir gecikme ekle
				await Application.Current!.Dispatcher.DispatchAsync(async () =>
				{
					InitializeLiveCharts();
					await shell.GoToAsync("///LoginPage");
				});
			});

			return window;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine("CreateWindow exception: " + ex);
			return new Window(new ContentPage { Content = new Label { Text = "Window Error: " + ex.ToString() } });
		}
	}

	protected override void OnStart()
	{
		// Handle when your app starts
	}

	protected override void OnSleep()
	{
		// Handle when your app sleeps
	}

	protected override void OnResume()
	{
		// Handle when your app resumes
	}
}