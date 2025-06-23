using System;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.Input;

namespace CryptoGuard.Core.Models
{
    public class Coin
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Symbol { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Price { get; set; }
        public decimal MarketCap { get; set; }
        public decimal PriceChangePercentage24h { get; set; }
        [NotMapped]
        public decimal PriceChangePercentage1h { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        [NotMapped]
        public int Index { get; set; }
        [NotMapped]
        public bool IsFavorite { get; set; }
        [NotMapped]
        public CommunityToolkit.Mvvm.Input.IRelayCommand<Coin> ToggleFavoriteCommand { get; set; }
    }
} 