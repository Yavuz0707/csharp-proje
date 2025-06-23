using System;

namespace CryptoGuard.Core.Models
{
    public class PriceAlert
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required User User { get; set; }
        public required string CoinId { get; set; }
        public required Coin Coin { get; set; }
        public decimal TargetPrice { get; set; }
        public bool IsTriggered { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TriggeredAt { get; set; }
    }
} 