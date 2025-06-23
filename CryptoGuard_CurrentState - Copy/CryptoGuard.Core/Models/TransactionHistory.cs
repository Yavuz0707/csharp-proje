using System;

namespace CryptoGuard.Core.Models
{
    public class TransactionHistory
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public string CoinId { get; set; }
        public TransactionType TransactionType { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }

        // Navigation properties
        public Portfolio Portfolio { get; set; }
        public Coin Coin { get; set; }
    }

    public enum TransactionType
    {
        Buy,
        Sell,
        Edit
    }
} 