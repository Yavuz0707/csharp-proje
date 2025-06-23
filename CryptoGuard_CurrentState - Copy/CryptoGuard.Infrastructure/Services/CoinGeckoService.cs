using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using System.Linq;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace CryptoGuard.Infrastructure.Services
{
    public class CoinLoreService : ICoinLoreService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CoinLoreService> _logger;

        public CoinLoreService(IConfiguration configuration, ILogger<CoinLoreService> logger)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.coinlore.net/api/");
        }

        public async Task<Coin?> GetCoinPriceAsync(string coinId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CoinLoreResponse>>($"ticker/?id={coinId}");
                if (response == null || !response.Any())
                {
                    // Kullanıcıya hata mesajı göster, uygulamayı döngüye sokma
                    return null;
                }
                var data = response[0];
                return new Coin
                {
                    Id = data.Id,
                    Name = data.Name,
                    Symbol = data.Symbol,
                    CurrentPrice = SafeParseDecimal(data.PriceUsd),
                    MarketCap = SafeParseDecimal(data.MarketCapUsd),
                    PriceChangePercentage24h = SafeParseDecimal(data.PercentChange24h),
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coin price for {CoinId}", coinId);
                // Kullanıcıya hata mesajı göster, uygulamayı döngüye sokma
                return null;
            }
        }

        public async Task<IEnumerable<Coin>> GetTopCoinsAsync(int limit = 100)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<CoinLoreTickersResponse>($"tickers/?start=0&limit={limit}");
                
                if (response == null || response.Data == null)
                {
                    throw new Exception("Failed to get top coins");
                }

                if (response.Data.Count > 0)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(response.Data[0]);
                    _logger.LogInformation($"DEBUG: İlk coin JSON: {json}");
                }

                return response.Data.Select(data => new Coin
                {
                    Id = data.Id,
                    Name = data.Name,
                    Symbol = data.Symbol,
                    CurrentPrice = SafeParseDecimal(data.PriceUsd),
                    MarketCap = SafeParseDecimal(data.MarketCapUsd),
                    PriceChangePercentage24h = SafeParseDecimal(data.PercentChange24h),
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top coins");
                throw;
            }
        }

        public async Task<decimal> GetCoinMarketCapAsync(string coinId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CoinLoreResponse>>($"ticker/?id={coinId}");
                
                if (response == null || !response.Any())
                {
                    throw new Exception($"Coin {coinId} not found");
                }

                return SafeParseDecimal(response[0].MarketCapUsd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market cap for {CoinId}", coinId);
                throw;
            }
        }

        public async Task<decimal> GetCoinPriceChangePercentage24hAsync(string coinId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<CoinLoreResponse>>($"ticker/?id={coinId}");
                
                if (response == null || !response.Any())
                {
                    throw new Exception($"Coin {coinId} not found");
                }

                return SafeParseDecimal(response[0].PercentChange24h);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting 24h price change for {CoinId}", coinId);
                throw;
            }
        }

        private decimal SafeParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            // Önce nokta ile dene
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                return result;
            // Sonra virgül ile dene
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out result))
                return result;
            return 0;
        }
    }

    public class CoinLoreResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("price_usd")]
        public string PriceUsd { get; set; } = string.Empty;
        [JsonPropertyName("market_cap_usd")]
        public string MarketCapUsd { get; set; } = string.Empty;
        [JsonPropertyName("percent_change_24h")]
        public string PercentChange24h { get; set; } = string.Empty;
        [JsonPropertyName("volume24")]
        public decimal? Volume24 { get; set; }
        [JsonPropertyName("rank")]
        public int? Rank { get; set; }
        [JsonPropertyName("tsupply")]
        public string Supply { get; set; } = string.Empty;
        [JsonPropertyName("msupply")]
        public string MaxSupply { get; set; } = string.Empty;
    }

    public class CoinLoreTickersResponse
    {
        public List<CoinLoreResponse> Data { get; set; } = new();
    }
} 