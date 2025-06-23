using System;

namespace CryptoGuard.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public string ProfilePhoto { get; set; } = "profile_placeholder.png";
        public string Biography { get; set; } = string.Empty;
        public bool IsPortfolioPublic { get; set; } = true;
    }
} 