using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using CryptoGuard.Core.Models;
using CryptoGuard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CryptoGuard.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IDbContextFactory<CryptoGuardDbContext> _dbContextFactory;

        public UserService(IRepository<User> userRepository, IDbContextFactory<CryptoGuardDbContext> dbContextFactory)
        {
            _userRepository = userRepository;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id.ToString());
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            System.Diagnostics.Debug.WriteLine($"UserService.GetUserByUsernameAsync: {username}");
            using var dbContext = _dbContextFactory.CreateDbContext();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            System.Diagnostics.Debug.WriteLine($"UserService.GetUserByUsernameAsync: user found = {(user != null ? user.Username : "null")}");
            return user;
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            // Check if username or email already exists
            var existingUser = await _userRepository.FindAsync(u => 
                u.Username == user.Username || u.Email == user.Email);

            if (existingUser.Any())
            {
                throw new Exception("Username or email already exists");
            }

            // Hash the password
            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(user.ProfilePhoto))
                user.ProfilePhoto = "profile_placeholder.png";
            if (string.IsNullOrWhiteSpace(user.Biography))
                user.Biography = "Henüz biyografi eklenmedi.";

            await _userRepository.AddAsync(user);
            return user;
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _userRepository.GetByIdAsync(user.Id.ToString());
            if (existingUser == null)
                throw new Exception("User not found");

            // Email değişiyorsa ve başka bir kullanıcıda bu email varsa hata ver
            if (!string.IsNullOrWhiteSpace(user.Email) && existingUser.Email != user.Email)
            {
                var emailUser = (await _userRepository.FindAsync(u => u.Email == user.Email)).FirstOrDefault();
                if (emailUser != null && emailUser.Id != user.Id)
                    throw new Exception("Bu email başka bir kullanıcıya ait!");
                existingUser.Email = user.Email;
            }
            if (!string.IsNullOrWhiteSpace(user.Username))
                existingUser.Username = user.Username;
            if (!string.IsNullOrWhiteSpace(user.ProfilePhoto))
                existingUser.ProfilePhoto = user.ProfilePhoto;
            else if (string.IsNullOrWhiteSpace(existingUser.ProfilePhoto))
                existingUser.ProfilePhoto = "profile_placeholder.png";
            if (!string.IsNullOrWhiteSpace(user.Biography))
                existingUser.Biography = user.Biography;
            else if (string.IsNullOrWhiteSpace(existingUser.Biography))
                existingUser.Biography = "Henüz biyografi eklenmedi.";
            existingUser.IsPortfolioPublic = user.IsPortfolioPublic;
            System.Diagnostics.Debug.WriteLine($"UserService.UpdateUserAsync: Updating user {existingUser.Id} with IsPortfolioPublic: {existingUser.IsPortfolioPublic}");
            await _userRepository.UpdateAsync(existingUser);
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id.ToString());
            if (user == null)
            {
                throw new Exception("User not found");
            }

            await _userRepository.DeleteAsync(user);
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user == null)
            {
                return false;
            }

            var hashedPassword = HashPassword(password);
            return user.PasswordHash == hashedPassword;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception("User not found");
            }

            var hashedCurrentPassword = HashPassword(currentPassword);
            if (user.PasswordHash != hashedCurrentPassword)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<User> Login(string username, string password)
        {
            var user = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı. Lütfen önce kayıt olun.");
            }
            var hashedPassword = HashPassword(password);
            if (user.PasswordHash != hashedPassword)
            {
                throw new Exception("Şifre yanlış.");
            }
            return user;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task<bool> FollowUserAsync(int followerId, int followingId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (followerId == followingId) return false;
            if (await dbContext.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId)) return false;
            dbContext.Follows.Add(new Follow { FollowerId = followerId, FollowingId = followingId, CreatedAt = DateTime.Now });
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowUserAsync(int followerId, int followingId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var follow = await dbContext.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
            if (follow == null) return false;
            dbContext.Follows.Remove(follow);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followingId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task<int> GetFollowersCountAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Follows.CountAsync(f => f.FollowingId == userId);
        }

        public async Task<int> GetFollowingCountAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Follows.CountAsync(f => f.FollowerId == userId);
        }

        public async Task<List<User>> GetFollowersAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Follows.Where(f => f.FollowingId == userId).Include(f => f.Follower).Select(f => f.Follower).ToListAsync();
        }

        public async Task<List<User>> GetFollowingAsync(int userId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Follows.Where(f => f.FollowerId == userId).Include(f => f.Following).Select(f => f.Following).ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Users.ToListAsync();
        }

        public async Task<User?> GetUserByUsernameAndEmail(string username, string email)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.Email == email);
        }

        public async Task<bool> ResetPasswordWithoutOldPasswordAsync(int userId, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
} 