using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CryptoGuard.Core.Models
{
    public class Portfolio
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastUpdated { get; set; }
        [JsonIgnore]
        public List<PortfolioItem> Items { get; set; } = new();
    }

    public class PortfolioItem
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        [JsonIgnore]
        public Portfolio? Portfolio { get; set; }
        public required string CoinId { get; set; }
        public Coin? Coin { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime LastUpdated { get; set; }

        // ViewModel için gerekli özellikler
        public string CoinName => Coin?.Name ?? CoinId;
        public string CoinSymbol => Coin?.Symbol ?? string.Empty;
        public decimal ProfitPercent => BuyPrice > 0 ? ((CurrentValue - (Quantity * BuyPrice)) / (Quantity * BuyPrice)) * 100 : 0;
    }
} 