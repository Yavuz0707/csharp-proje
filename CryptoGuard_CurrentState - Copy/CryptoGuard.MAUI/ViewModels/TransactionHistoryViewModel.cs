using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoGuard.Core.Models;
using CryptoGuard.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace CryptoGuard.MAUI.ViewModels
{
    public class TransactionHistoryViewModel : ObservableObject
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ICoinLoreService _coinLoreService;
        public ObservableCollection<TransactionHistoryRow> Transactions { get; set; } = new();

        public TransactionHistoryViewModel(IPortfolioService portfolioService, ICoinLoreService coinLoreService)
        {
            _portfolioService = portfolioService;
            _coinLoreService = coinLoreService;
            LoadTransactions();
        }

        public TransactionHistoryViewModel() { }

        private async void LoadTransactions()
        {
            Transactions.Clear();
            int userId = UserContext.CurrentUserId ?? 0;
            var history = await _portfolioService.GetTransactionHistoryByUserIdAsync(userId);
            var coinIds = history.Select(h => h.CoinId).Distinct().ToList();
            var coinDict = new Dictionary<string, string>();
            foreach (var coinId in coinIds)
            {
                try
                {
                    var coin = await _coinLoreService.GetCoinPriceAsync(coinId);
                    if (coin == null)
                    {
                        // Kullanıcıya hata mesajı göster
                        return;
                    }
                    coinDict[coinId] = coin.Symbol;
                }
                catch { coinDict[coinId] = coinId; }
            }
            foreach (var item in history)
            {
                string coinSymbol = coinDict.ContainsKey(item.CoinId) ? coinDict[item.CoinId] : item.CoinId;
                string islemText = item.TransactionType switch
                {
                    TransactionType.Buy => "Alış",
                    TransactionType.Sell => "Satış",
                    TransactionType.Edit => "Düzenleme",
                    _ => item.TransactionType.ToString()
                };
                decimal realizedProfit = 0;
                decimal realizedProfitPercent = 0;
                if (item.TransactionType == TransactionType.Sell || item.TransactionType == TransactionType.Edit)
                {
                    var buys = history.Where(h => h.CoinId == item.CoinId && h.TransactionType == TransactionType.Buy && h.TransactionDate <= item.TransactionDate).ToList();
                    decimal totalBuy = buys.Sum(b => b.TotalAmount);
                    if (totalBuy > 0)
                    {
                        realizedProfit = item.TotalAmount - totalBuy;
                        realizedProfitPercent = (realizedProfit / totalBuy) * 100;
                    }
                }
                Transactions.Add(new TransactionHistoryRow
                {
                    TransactionDate = item.TransactionDate,
                    CoinId = item.CoinId,
                    CoinSymbol = coinSymbol,
                    TransactionType = item.TransactionType,
                    IslemText = islemText,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    RealizedProfit = realizedProfit,
                    RealizedProfitPercent = realizedProfitPercent
                });
            }
        }
    }

    public class TransactionHistoryRow
    {
        public DateTime TransactionDate { get; set; }
        public string CoinId { get; set; }
        public string CoinSymbol { get; set; }
        public TransactionType TransactionType { get; set; }
        public string IslemText { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal RealizedProfit { get; set; }
        public decimal RealizedProfitPercent { get; set; }
        public string RealizedProfitDisplay => (TransactionType == TransactionType.Buy) ? "-" : $"{RealizedProfit:N2} ({RealizedProfitPercent:N2}%)";
    }
} 