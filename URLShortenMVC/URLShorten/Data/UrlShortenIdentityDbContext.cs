using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using URLShorten.Commons;
using URLShorten.Data.Entities;
using URLShorten.Data.IdentityEntities;

namespace URLShorten.Data
{
    public class UrlShortenIdentityDbContext : IdentityDbContext<UrlLinksUser>
    {
        public UrlShortenIdentityDbContext(DbContextOptions<UrlShortenIdentityDbContext> options) : base(options)
        {
        }

        public DbSet<UrlLinksUser> UrlLinksUsers{ get; set; }
        public DbSet<UrlLinksRole> UrlLinksRoles{ get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UrlLinksUser>()
                .Property(p => p.FullName)
                .HasMaxLength(MaxLengths.FullName);
            
            modelBuilder.Entity<UrlLinksUser>()
                .Property(p => p.Avatar)
                .HasMaxLength(MaxLengths.FileName);
           

            modelBuilder.Entity<UrlLinksRole>()
               .Property(p => p.Description)
               .HasMaxLength(MaxLengths.Description);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (!optionsBuilder.IsConfigured)
            //{
            //    var connectionString = "Server=10.25.32.169, 1433;Database=TheaterIdentityDbContext; User Id=sa; password=Riolish@12345; TrustServerCertificate=True; MultipleActiveResultSets=true;";
            //    optionsBuilder.UseSqlServer(connectionString);
            //}
        }

    }
}
