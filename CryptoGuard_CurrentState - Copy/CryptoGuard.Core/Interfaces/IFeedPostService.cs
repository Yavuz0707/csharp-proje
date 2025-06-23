using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGuard.Core.Models;

namespace CryptoGuard.Core.Interfaces
{
    public interface IFeedPostService
    {
        Task AddPostAsync(FeedPost post);
        Task<List<FeedPost>> GetAllPostsAsync();
        Task<List<FeedPost>> GetPostsByUserIdAsync(int userId);
        Task DeletePostAsync(int postId);
        Task UpdatePostAsync(FeedPost post);
        Task LikePostAsync(int postId, int userId);
        Task UnlikePostAsync(int postId, int userId);
        Task AddCommentAsync(int postId, int userId, string content);
        Task<List<Comment>> GetCommentsAsync(int postId);
        Task<List<Like>> GetLikesAsync(int postId);
        Task<Dictionary<string, int>> GetTrendingHashtagsAsync(int top = 10);
    }
} 