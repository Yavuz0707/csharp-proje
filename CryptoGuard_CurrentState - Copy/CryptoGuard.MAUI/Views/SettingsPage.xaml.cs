using CryptoGuard.MAUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoGuard.MAUI.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() : this(App.ServiceProvider.GetRequiredService<SettingsViewModel>()) { }

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 