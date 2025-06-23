using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using CryptoGuard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CryptoGuard.Services.Services
{
    public class FeedPostService : IFeedPostService
    {
        private readonly IDbContextFactory<CryptoGuardDbContext> _dbContextFactory;

        public FeedPostService(IDbContextFactory<CryptoGuardDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task AddPostAsync(FeedPost post)
        {
            if (post.ImagePath == null)
                post.ImagePath = "";
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.FeedPosts.Add(post);
            await dbContext.SaveChangesAsync();
        }

        public async Task<List<FeedPost>> GetAllPostsAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.FeedPosts
                .Include(p => p.User)
                .Include(p => p.OriginalPost)
                .ThenInclude(op => op.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<FeedPost>> GetPostsByUserIdAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.FeedPosts.Include(p => p.User).Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task DeletePostAsync(int postId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var post = await dbContext.FeedPosts.FindAsync(postId);
            if (post != null)
            {
                dbContext.FeedPosts.Remove(post);
                await dbContext.SaveChangesAsync();
            }
            // Alıntılayan gönderiler silinmez!
        }

        public async Task UpdatePostAsync(FeedPost post)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = await dbContext.FeedPosts.FindAsync(post.Id);
            if (existing != null)
            {
                existing.Content = post.Content;
                existing.CoinTag = post.CoinTag;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task LikePostAsync(int postId, int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var post = await dbContext.FeedPosts.FindAsync(postId);
            if (post == null) return;
            if (dbContext.Likes.Any(l => l.PostId == postId && l.UserId == userId)) return;
            var like = new Like { PostId = postId, UserId = userId, CreatedAt = DateTime.Now };
            dbContext.Likes.Add(like);
            post.LikeCount++;
            await dbContext.SaveChangesAsync();
        }

        public async Task UnlikePostAsync(int postId, int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var like = dbContext.Likes.FirstOrDefault(l => l.PostId == postId && l.UserId == userId);
            if (like == null) return;
            var post = await dbContext.FeedPosts.FindAsync(postId);
            dbContext.Likes.Remove(like);
            if (post != null && post.LikeCount > 0) post.LikeCount--;
            await dbContext.SaveChangesAsync();
        }

        public async Task AddCommentAsync(int postId, int userId, string content)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var post = await dbContext.FeedPosts.FindAsync(postId);
            if (post == null) return;
            var comment = new Comment { PostId = postId, UserId = userId, Content = content, CreatedAt = DateTime.Now };
            dbContext.Comments.Add(comment);
            post.CommentCount++;
            await dbContext.SaveChangesAsync();
        }

        public async Task<List<Comment>> GetCommentsAsync(int postId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Comments.Include(c => c.User).Where(c => c.PostId == postId).OrderBy(c => c.CreatedAt).ToListAsync();
        }

        public async Task<List<Like>> GetLikesAsync(int postId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Likes.Include(l => l.User).Where(l => l.PostId == postId).ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetTrendingHashtagsAsync(int top = 10)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var posts = await dbContext.FeedPosts.ToListAsync();
            var hashtagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var regex = new Regex(@"#(\w+)");
            foreach (var post in posts)
            {
                if (string.IsNullOrWhiteSpace(post.Content)) continue;
                var matches = regex.Matches(post.Content);
                foreach (Match match in matches)
                {
                    var tag = match.Value;
                    if (hashtagCounts.ContainsKey(tag))
                        hashtagCounts[tag]++;
                    else
                        hashtagCounts[tag] = 1;
                }
            }
            return hashtagCounts
                .OrderByDescending(kv => kv.Value)
                .Take(top)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
} 