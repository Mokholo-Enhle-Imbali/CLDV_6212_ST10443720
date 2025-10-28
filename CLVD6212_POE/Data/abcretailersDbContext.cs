using CLVD6212_POE.Models;
using Microsoft.EntityFrameworkCore;

namespace CLVD6212_POE.Data
{
    public class abcretailersDbContext: DbContext
    {
        public abcretailersDbContext(DbContextOptions<abcretailersDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Cart> Cart => Set<Cart>();
        public DbSet<Order> Orders => Set<Order>();

        

    }
}
