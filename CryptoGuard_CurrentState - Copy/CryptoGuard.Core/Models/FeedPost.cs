using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CryptoGuard.Core.Models
{
    public class FeedPost
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public string CoinTag { get; set; }
        public DateTime CreatedAt { get; set; }
        [JsonIgnore]
        public User User { get; set; } // Navigation property
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsMine { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool HasCoinTag => !string.IsNullOrWhiteSpace(CoinTag);
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        [JsonIgnore]
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsLikedByMe { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsLiked { get; set; }
        public string ImagePath { get; set; } // Gönderiye ait resim yolu
        public int? OriginalPostId { get; set; }
        public FeedPost OriginalPost { get; set; }
    }
} 