using IC.Common.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PasswordCredentials.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<UserAccount>, IDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions)
            : base(dbContextOptions)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserAccount>(t =>
            {
                t.ToTable(nameof(UserAccount));
            });
        }
    }
}
