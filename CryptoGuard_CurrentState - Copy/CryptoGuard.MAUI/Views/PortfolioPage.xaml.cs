using CryptoGuard.MAUI.ViewModels;
using CryptoGuard.Infrastructure.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Linq;
using CryptoGuard.Core.Models;
using LiveChartsCore.SkiaSharpView.Maui;
using LiveChartsCore.Measure;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;

namespace CryptoGuard.MAUI.Views;

public partial class PortfolioPage : ContentPage
{
    private readonly PortfolioViewModel _viewModel;
    private readonly CryptoGuardAIViewModel _aiViewModel;

    public PortfolioPage(PortfolioViewModel viewModel, CryptoGuardAIViewModel aiViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _aiViewModel = aiViewModel;
        BindingContext = _viewModel;
    }

    public PortfolioPage() : this(
        App.ServiceProvider.GetRequiredService<PortfolioViewModel>(),
        App.ServiceProvider.GetRequiredService<CryptoGuardAIViewModel>()) { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPortfolioData();
        pieChart.LegendTextPaint = new SolidColorPaint(SKColor.Parse("#b4befe"));
    }

    private void OnCoinSelected(object sender, SelectionChangedEventArgs e)
    {
        if (BindingContext is PortfolioViewModel vm && e.CurrentSelection.FirstOrDefault() is PortfolioItemViewModel item)
        {
            vm.ShowCoinDetailCommand.Execute(item);
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnAddCoinClicked(object sender, EventArgs e)
    {
        var popupVm = new AddCoinPopupViewModel(_viewModel.PortfolioService, _viewModel.CoinGeckoService);
        var popup = new AddCoinPopup(popupVm);
        popupVm.OnCompleted += async () =>
        {
            await _viewModel.LoadPortfolioData();
        };
        await Navigation.PushModalAsync(popup);
    }

    private async void OnAIAssistantClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CryptoGuardAIPage(_aiViewModel));
    }

    private void OnToggleCurrencyClicked(object sender, EventArgs e)
    {
        _viewModel.ShowInTL = !_viewModel.ShowInTL;
    }

    private async void OnShowHistoryClicked(object sender, EventArgs e)
    {
        var service = _viewModel.PortfolioService;
        var coinLoreService = _viewModel.CoinGeckoService;
        var page = new TransactionHistoryPage(service, coinLoreService);
        await Navigation.PushAsync(page);
    }

    private async void OnGetReportClicked(object sender, EventArgs e)
    {
        var service = _viewModel.PortfolioService;
        int userId = UserContext.CurrentUserId ?? 0;
        var transactions = await service.GetTransactionHistoryByUserIdAsync(userId);
        decimal totalBuy = transactions.Where(x => x.TransactionType == CryptoGuard.Core.Models.TransactionType.Buy).Sum(x => x.TotalAmount);
        decimal totalSell = transactions.Where(x => x.TransactionType == CryptoGuard.Core.Models.TransactionType.Sell || x.TransactionType == CryptoGuard.Core.Models.TransactionType.Edit).Sum(x => x.TotalAmount);
        decimal profit = totalSell - totalBuy;
        string rapor = $"Toplam Alış: {totalBuy:N2}\nToplam Satış: {totalSell:N2}\nNet Kar/Zarar: {profit:N2}";
        await DisplayAlert("Kar/Zarar Raporu", rapor, "Tamam");
    }
} 