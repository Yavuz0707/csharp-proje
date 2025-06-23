using CryptoGuard.MAUI.ViewModels;
using CryptoGuard.Core.Interfaces;
using Microsoft.Maui.Controls;

namespace CryptoGuard.MAUI.Views;

public partial class TransactionHistoryPage : ContentPage
{
    public TransactionHistoryPage(IPortfolioService portfolioService, ICoinLoreService coinLoreService)
    {
        InitializeComponent();
        BindingContext = new TransactionHistoryViewModel(portfolioService, coinLoreService);
    }
} 