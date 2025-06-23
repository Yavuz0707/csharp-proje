using System;
using System.Text.Json.Serialization;

namespace CryptoGuard.Core.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        [JsonIgnore]
        public User User { get; set; }
        [JsonIgnore]
        public FeedPost FeedPost { get; set; }
    }
} 