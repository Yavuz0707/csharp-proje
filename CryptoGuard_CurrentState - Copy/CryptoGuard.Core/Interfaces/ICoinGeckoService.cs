using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGuard.Core.Models;

namespace CryptoGuard.Core.Interfaces
{
    public interface ICoinLoreService
    {
        Task<Coin?> GetCoinPriceAsync(string coinId);
        Task<IEnumerable<Coin>> GetTopCoinsAsync(int limit = 100);
        Task<decimal> GetCoinMarketCapAsync(string coinId);
        Task<decimal> GetCoinPriceChangePercentage24hAsync(string coinId);
    }
} 