namespace CryptoGuard.Core.Models
{
    public class NewsItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string ImgUrl { get; set; } = string.Empty;
        public string PubDate { get; set; } = string.Empty;
    }
} 