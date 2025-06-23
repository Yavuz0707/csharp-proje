using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CryptoGuard.Infrastructure.Data
{
    public class CryptoGuardDbContextFactory : IDesignTimeDbContextFactory<CryptoGuardDbContext>
    {
        public CryptoGuardDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<CryptoGuardDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new CryptoGuardDbContext(optionsBuilder.Options);
        }
    }
} 