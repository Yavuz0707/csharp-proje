using System;

namespace CryptoGuard.Core.Models
{
    public class FavoriteCoin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CoinId { get; set; }
    }
} 