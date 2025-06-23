using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using CryptoGuard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoGuard.Services.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IRepository<Portfolio> _portfolioRepository;
        private readonly IRepository<PortfolioItem> _portfolioItemRepository;
        private readonly IRepository<Coin> _coinRepository;
        private readonly IRepository<TransactionHistory> _transactionHistoryRepository;
        private readonly ICoinLoreService _coinLoreService;
        private readonly IDbContextFactory<CryptoGuardDbContext> _dbContextFactory;

        public PortfolioService(
            IRepository<Portfolio> portfolioRepository,
            IRepository<PortfolioItem> portfolioItemRepository,
            IRepository<Coin> coinRepository,
            IRepository<TransactionHistory> transactionHistoryRepository,
            ICoinLoreService coinLoreService,
            IDbContextFactory<CryptoGuardDbContext> dbContextFactory)
        {
            _portfolioRepository = portfolioRepository;
            _portfolioItemRepository = portfolioItemRepository;
            _coinRepository = coinRepository;
            _transactionHistoryRepository = transactionHistoryRepository;
            _coinLoreService = coinLoreService;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Portfolio> GetPortfolioByIdAsync(int id)
        {
            return await _portfolioRepository.GetByIdAsync(id.ToString());
        }

        public async Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(int userId)
        {
            // Eager loading ile Items'ı da getir
            var portfolios = _portfolioRepository.GetAllIncluding(p => p.Items)
                .Where(p => p.UserId == userId);
            return await portfolios.ToListAsync();
        }

        public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
        {
            portfolio.LastUpdated = DateTime.UtcNow;
            portfolio.TotalValue = 0;

            await _portfolioRepository.AddAsync(portfolio);
            return portfolio;
        }

        public async Task UpdatePortfolioAsync(Portfolio portfolio)
        {
            var existingPortfolio = await _portfolioRepository.GetByIdAsync(portfolio.Id.ToString());
            if (existingPortfolio == null)
            {
                throw new Exception("Portfolio not found");
            }

            existingPortfolio.Name = portfolio.Name;
            existingPortfolio.LastUpdated = DateTime.UtcNow;

            await _portfolioRepository.UpdateAsync(existingPortfolio);
        }

        public async Task DeletePortfolioAsync(int id)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(id.ToString());
            if (portfolio == null)
            {
                throw new Exception("Portfolio not found");
            }

            await _portfolioRepository.DeleteAsync(portfolio);
        }

        public async Task<decimal> CalculatePortfolioValueAsync(int portfolioId)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId.ToString());
            if (portfolio == null)
            {
                throw new Exception("Portfolio not found");
            }

            var items = await _portfolioItemRepository.FindAsync(pi => pi.PortfolioId == portfolioId);
            decimal totalValue = 0;

            foreach (var item in items)
            {
                var coin = await _coinRepository.GetByIdAsync(item.CoinId);
                if (coin != null)
                {
                    totalValue += item.Quantity * coin.CurrentPrice;
                }
            }

            portfolio.TotalValue = totalValue;
            portfolio.LastUpdated = DateTime.UtcNow;
            await _portfolioRepository.UpdateAsync(portfolio);

            return totalValue;
        }

        public async Task AddCoinToPortfolioAsync(int portfolioId, string coinId, decimal quantity, decimal buyPrice)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId.ToString());
            if (portfolio == null)
                throw new Exception("Portfolio not found");

            // Coin veritabanında var mı kontrol et (FindAsync ile LINQ sorgusu)
            var coin = (await _coinRepository.FindAsync(c => c.Id == coinId)).FirstOrDefault();
            if (coin == null)
            {
                // CoinLore'dan çek ve ekle
                var coinFromApi = await _coinLoreService.GetCoinPriceAsync(coinId);
                if (coinFromApi == null)
                {
                    // Kullanıcıya hata mesajı göster
                    return;
                }
                coinFromApi.Id = coinId;
                try
                {
                    await _coinRepository.AddAsync(coinFromApi);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Coin eklenirken hata: {ex.Message}. CoinId: {coinId}", ex);
                }
                // Eklendikten sonra tekrar kontrol et
                var allCoinsAfter = await _coinRepository.GetAllAsync();
                if (!allCoinsAfter.Any(c => c.Id == coinId))
                {
                    throw new Exception($"Coin eklenmesine rağmen veritabanında bulunamadı! CoinId: {coinId} Sonraki coinler: {string.Join(",", allCoinsAfter.Select(c => c.Id))}");
                }
                coin = coinFromApi;
            }

            // Portföy item ekle
            var existingItem = (await _portfolioItemRepository.FindAsync(pi => pi.PortfolioId == portfolioId && pi.CoinId == coinId)).FirstOrDefault();
            if (existingItem != null)
            {
                var totalQuantity = existingItem.Quantity + quantity;
                var totalValue = (existingItem.Quantity * existingItem.BuyPrice) + (quantity * buyPrice);
                existingItem.BuyPrice = totalValue / totalQuantity;
                existingItem.Quantity = totalQuantity;
                existingItem.LastUpdated = DateTime.UtcNow;
                await _portfolioItemRepository.UpdateAsync(existingItem);
            }
            else
            {
                var newItem = new PortfolioItem
                {
                    PortfolioId = portfolioId,
                    CoinId = coinId,
                    Quantity = quantity,
                    BuyPrice = buyPrice,
                    PurchaseDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                await _portfolioItemRepository.AddAsync(newItem);
            }

            // Add transaction history
            var transaction = new TransactionHistory
            {
                PortfolioId = portfolioId,
                CoinId = coinId,
                TransactionType = TransactionType.Buy,
                Quantity = quantity,
                Price = buyPrice,
                TransactionDate = DateTime.UtcNow,
                TotalAmount = quantity * buyPrice
            };
            await _transactionHistoryRepository.AddAsync(transaction);

            await CalculatePortfolioValueAsync(portfolioId);
        }

        public async Task RemoveCoinFromPortfolioAsync(int portfolioId, string coinId)
        {
            var item = (await _portfolioItemRepository.FindAsync(pi => 
                pi.PortfolioId == portfolioId && pi.CoinId == coinId)).FirstOrDefault();

            if (item == null)
            {
                throw new Exception("Portfolio item not found");
            }

            // Add transaction history before removing the item
            var transaction = new TransactionHistory
            {
                PortfolioId = portfolioId,
                CoinId = coinId,
                TransactionType = TransactionType.Sell,
                Quantity = item.Quantity,
                Price = item.BuyPrice,
                TransactionDate = DateTime.UtcNow,
                TotalAmount = item.Quantity * item.BuyPrice
            };
            await _transactionHistoryRepository.AddAsync(transaction);

            await _portfolioItemRepository.DeleteAsync(item);
            await CalculatePortfolioValueAsync(portfolioId);
        }

        public async Task UpdatePortfolioItemQuantityAsync(int portfolioId, string coinId, decimal newQuantity)
        {
            var item = (await _portfolioItemRepository.FindAsync(pi => 
                pi.PortfolioId == portfolioId && pi.CoinId == coinId)).FirstOrDefault();

            if (item == null)
            {
                throw new Exception("Portfolio item not found");
            }

            // Düzenleme işlemi geçmişe kaydedilsin
            var transaction = new TransactionHistory
            {
                PortfolioId = portfolioId,
                CoinId = coinId,
                TransactionType = TransactionType.Edit,
                Quantity = newQuantity,
                Price = item.BuyPrice,
                TransactionDate = DateTime.UtcNow,
                TotalAmount = newQuantity * item.BuyPrice
            };
            await _transactionHistoryRepository.AddAsync(transaction);

            item.Quantity = newQuantity;
            item.LastUpdated = DateTime.UtcNow;

            await _portfolioItemRepository.UpdateAsync(item);
            await CalculatePortfolioValueAsync(portfolioId);
        }

        public async Task<IEnumerable<PortfolioItem>> GetPortfolioItemsByUserIdAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var portfolioIds = await dbContext.Portfolios
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var items = await dbContext.PortfolioItems
                .Include(pi => pi.Coin)
                .Include(pi => pi.Portfolio)
                .Where(pi => portfolioIds.Contains(pi.PortfolioId))
                .ToListAsync();

            return items;
        }

        public async Task UpdatePortfolioItemAsync(int portfolioItemId, decimal newQuantity, decimal newBuyPrice)
        {
            var item = (await _portfolioItemRepository.FindAsync(i => i.Id == portfolioItemId)).FirstOrDefault();
            if (item == null)
                throw new Exception("Portfolio item not found");

            // Düzenleme işlemi geçmişe kaydedilsin
            var transaction = new TransactionHistory
            {
                PortfolioId = item.PortfolioId,
                CoinId = item.CoinId,
                TransactionType = TransactionType.Edit,
                Quantity = newQuantity,
                Price = newBuyPrice,
                TransactionDate = DateTime.UtcNow,
                TotalAmount = newQuantity * newBuyPrice
            };
            await _transactionHistoryRepository.AddAsync(transaction);

            item.Quantity = newQuantity;
            item.BuyPrice = newBuyPrice;
            item.LastUpdated = DateTime.UtcNow;
            await _portfolioItemRepository.UpdateAsync(item);
            await CalculatePortfolioValueAsync(item.PortfolioId);
        }

        public async Task DeletePortfolioItemAsync(int portfolioItemId)
        {
            var item = (await _portfolioItemRepository.FindAsync(i => i.Id == portfolioItemId)).FirstOrDefault();
            if (item == null)
                throw new Exception("Portfolio item not found");
            await _portfolioItemRepository.DeleteAsync(item);
            await CalculatePortfolioValueAsync(item.PortfolioId);
        }

        public async Task<Portfolio> GetPortfolio(int userId)
        {
            return await _portfolioRepository.GetAll().FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<IEnumerable<TransactionHistory>> GetTransactionHistoryByUserIdAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var portfolioIds = await dbContext.Portfolios
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();
            var transactions = await dbContext.TransactionHistories
                .Where(th => portfolioIds.Contains(th.PortfolioId))
                .OrderByDescending(th => th.TransactionDate)
                .ToListAsync();
            return transactions;
        }

        public async Task InsertExampleTransactionsForUserAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var portfolio = await dbContext.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId);
            if (portfolio == null)
            {
                portfolio = new Portfolio { Name = "Demo Portföy", UserId = userId, LastUpdated = DateTime.UtcNow, TotalValue = 0 };
                dbContext.Portfolios.Add(portfolio);
                await dbContext.SaveChangesAsync();
            }
            var portfolioId = portfolio.Id;
            var coinIds = new[] { "btc", "eth", "bnb" };
            var random = new Random();
            var baseDate = DateTime.UtcNow.Date.AddDays(-5);
            var transactions = new List<TransactionHistory>();
            for (int i = 0; i < 5; i++)
            {
                var date = baseDate.AddDays(i);
                var coinId = coinIds[i % coinIds.Length];
                var quantity = (decimal)(random.NextDouble() * 0.5 + 0.1);
                var price = (decimal)(random.Next(20000, 50000));
                transactions.Add(new TransactionHistory
                {
                    PortfolioId = portfolioId,
                    CoinId = coinId,
                    TransactionType = TransactionType.Buy,
                    Quantity = quantity,
                    Price = price,
                    TotalAmount = quantity * price,
                    TransactionDate = date.AddHours(10)
                });
                // Satış işlemi de ekle (bazı günler)
                if (i % 2 == 1)
                {
                    var sellQuantity = quantity / 2;
                    transactions.Add(new TransactionHistory
                    {
                        PortfolioId = portfolioId,
                        CoinId = coinId,
                        TransactionType = TransactionType.Sell,
                        Quantity = sellQuantity,
                        Price = price * 1.05m,
                        TotalAmount = sellQuantity * price * 1.05m,
                        TransactionDate = date.AddHours(16)
                    });
                }
            }
            // Coinler veritabanında yoksa ekle
            foreach (var coinId in coinIds)
            {
                var coinExists = await dbContext.Coins.AnyAsync(c => c.Id == coinId);
                if (!coinExists)
                {
                    dbContext.Coins.Add(new Coin
                    {
                        Id = coinId,
                        Name = coinId.ToUpper(),
                        Symbol = coinId.ToUpper(),
                        CurrentPrice = random.Next(20000, 50000),
                        LastUpdated = DateTime.UtcNow,
                        MarketCap = 0,
                        Price = random.Next(20000, 50000),
                        PriceChangePercentage24h = 0,
                        ImageUrl = ""
                    });
                }
            }
            await dbContext.SaveChangesAsync();
            // Sonra işlemleri ekle
            dbContext.TransactionHistories.AddRange(transactions);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddFavoriteCoinAsync(int userId, string coinId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (!await dbContext.FavoriteCoins.AnyAsync(f => f.UserId == userId && f.CoinId == coinId))
            {
                dbContext.FavoriteCoins.Add(new FavoriteCoin { UserId = userId, CoinId = coinId });
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveFavoriteCoinAsync(int userId, string coinId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var fav = await dbContext.FavoriteCoins.FirstOrDefaultAsync(f => f.UserId == userId && f.CoinId == coinId);
            if (fav != null)
            {
                dbContext.FavoriteCoins.Remove(fav);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetFavoriteCoinsAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.FavoriteCoins.Where(f => f.UserId == userId).Select(f => f.CoinId).ToListAsync();
        }
    }
} 