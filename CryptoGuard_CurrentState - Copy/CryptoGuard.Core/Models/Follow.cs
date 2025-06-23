using System;

namespace CryptoGuard.Core.Models
{
    public class Follow
    {
        public int Id { get; set; }
        public int FollowerId { get; set; } // Takip eden
        public int FollowingId { get; set; } // Takip edilen
        public DateTime CreatedAt { get; set; }

        public User Follower { get; set; }
        public User Following { get; set; }
    }
} 