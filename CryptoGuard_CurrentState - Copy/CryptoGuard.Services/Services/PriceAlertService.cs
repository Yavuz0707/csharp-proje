using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;

namespace CryptoGuard.Services.Services
{
    public class PriceAlertService : IPriceAlertService
    {
        private readonly IRepository<PriceAlert> _alertRepository;
        private readonly IRepository<Coin> _coinRepository;
        private readonly ICoinLoreService _coinLoreService;

        public PriceAlertService(
            IRepository<PriceAlert> alertRepository,
            IRepository<Coin> coinRepository,
            ICoinLoreService coinLoreService)
        {
            _alertRepository = alertRepository;
            _coinRepository = coinRepository;
            _coinLoreService = coinLoreService;
        }

        public async Task<PriceAlert> GetAlertByIdAsync(int id)
        {
            return await _alertRepository.GetByIdAsync(id.ToString());
        }

        public async Task<IEnumerable<PriceAlert>> GetUserAlertsAsync(int userId)
        {
            var alerts = await _alertRepository.FindAsync(a => a.UserId == userId);
            return alerts;
        }

        public async Task<PriceAlert> CreateAlertAsync(PriceAlert alert)
        {
            var coin = await _coinRepository.GetByIdAsync(alert.CoinId);
            if (coin == null)
            {
                throw new Exception("Coin not found");
            }

            alert.CreatedAt = DateTime.UtcNow;
            alert.IsTriggered = false;
            alert.TriggeredAt = null;

            await _alertRepository.AddAsync(alert);
            return alert;
        }

        public async Task UpdateAlertAsync(PriceAlert alert)
        {
            var existingAlert = await _alertRepository.GetByIdAsync(alert.Id.ToString());
            if (existingAlert == null)
            {
                throw new Exception("Alert not found");
            }

            existingAlert.TargetPrice = alert.TargetPrice;

            await _alertRepository.UpdateAsync(existingAlert);
        }

        public async Task DeleteAlertAsync(int id)
        {
            var alert = await _alertRepository.GetByIdAsync(id.ToString());
            if (alert == null)
            {
                throw new Exception("Alert not found");
            }

            await _alertRepository.DeleteAsync(alert);
        }

        public async Task CheckAlertsAsync()
        {
            var activeAlerts = await _alertRepository.FindAsync(a => !a.IsTriggered);
            
            foreach (var alert in activeAlerts)
            {
                var coin = await _coinRepository.GetByIdAsync(alert.CoinId);
                if (coin == null) continue;

                var currentPrice = coin.CurrentPrice;
                bool isTriggered = currentPrice >= alert.TargetPrice;

                if (isTriggered)
                {
                    alert.IsTriggered = true;
                    alert.TriggeredAt = DateTime.UtcNow;
                    await _alertRepository.UpdateAsync(alert);
                }
            }
        }

        public async Task<IEnumerable<PriceAlert>> GetTriggeredAlertsAsync(int userId)
        {
            var alerts = await _alertRepository.FindAsync(a => 
                a.UserId == userId && a.IsTriggered);
            return alerts;
        }

        public async Task<bool> IsAlertTriggeredAsync(int alertId)
        {
            var alert = await _alertRepository.GetByIdAsync(alertId.ToString());
            if (alert == null)
            {
                throw new Exception("Alert not found");
            }

            return alert.IsTriggered;
        }
    }
} 