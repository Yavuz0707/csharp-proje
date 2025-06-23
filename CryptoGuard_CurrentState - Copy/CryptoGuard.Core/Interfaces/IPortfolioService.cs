using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGuard.Core.Models;

namespace CryptoGuard.Core.Interfaces
{
    public interface IPortfolioService
    {
        Task<Portfolio> GetPortfolioByIdAsync(int id);
        Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(int userId);
        Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio);
        Task UpdatePortfolioAsync(Portfolio portfolio);
        Task DeletePortfolioAsync(int id);
        Task<decimal> CalculatePortfolioValueAsync(int portfolioId);
        Task AddCoinToPortfolioAsync(int portfolioId, string coinId, decimal quantity, decimal buyPrice);
        Task RemoveCoinFromPortfolioAsync(int portfolioId, string coinId);
        Task UpdatePortfolioItemQuantityAsync(int portfolioId, string coinId, decimal newQuantity);
        Task<IEnumerable<PortfolioItem>> GetPortfolioItemsByUserIdAsync(int userId);
        Task UpdatePortfolioItemAsync(int portfolioItemId, decimal newQuantity, decimal newBuyPrice);
        Task DeletePortfolioItemAsync(int portfolioItemId);
        Task<Portfolio> GetPortfolio(int userId);
        Task<IEnumerable<TransactionHistory>> GetTransactionHistoryByUserIdAsync(int userId);
        Task InsertExampleTransactionsForUserAsync(int userId);
        Task AddFavoriteCoinAsync(int userId, string coinId);
        Task RemoveFavoriteCoinAsync(int userId, string coinId);
        Task<List<string>> GetFavoriteCoinsAsync(int userId);
    }
} 