using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGuard.Core.Models;

namespace CryptoGuard.Core.Interfaces
{
    public interface IPriceAlertService
    {
        Task<PriceAlert> GetAlertByIdAsync(int id);
        Task<IEnumerable<PriceAlert>> GetUserAlertsAsync(int userId);
        Task<PriceAlert> CreateAlertAsync(PriceAlert alert);
        Task UpdateAlertAsync(PriceAlert alert);
        Task DeleteAlertAsync(int id);
        Task CheckAlertsAsync();
        Task<IEnumerable<PriceAlert>> GetTriggeredAlertsAsync(int userId);
        Task<bool> IsAlertTriggeredAsync(int alertId);
    }
} 