using CryptoGuard.Core.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoGuard.Services.Services
{
    public class NewsService
    {
        private const string ApiKey = "e866366f685846f03a6f9c7d3867e28b";
        private const string ApiUrl = "https://gnews.io/api/v4/search?q=crypto&lang=tr&token=" + ApiKey;

        public async Task<List<NewsItem>> GetLatestNewsAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync(ApiUrl);
            var json = JsonDocument.Parse(response);
            var newsList = new List<NewsItem>();
            if (json.RootElement.TryGetProperty("articles", out var articlesArray))
            {
                foreach (var item in articlesArray.EnumerateArray())
                {
                    newsList.Add(new NewsItem
                    {
                        Id = item.GetProperty("url").GetString() ?? string.Empty,
                        Title = item.GetProperty("title").GetString() ?? string.Empty,
                        Description = item.GetProperty("description").GetString() ?? string.Empty,
                        Link = item.GetProperty("url").GetString() ?? string.Empty,
                        Source = item.TryGetProperty("source", out var src) && src.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                        ImgUrl = item.TryGetProperty("image", out var img) ? img.GetString() ?? string.Empty : string.Empty,
                        PubDate = item.TryGetProperty("publishedAt", out var pub) ? pub.GetString() ?? string.Empty : string.Empty
                    });
                }
            }
            return newsList;
        }
    }
} 