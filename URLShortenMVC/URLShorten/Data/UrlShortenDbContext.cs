using Microsoft.EntityFrameworkCore;
using URLShorten.Commons;
using URLShorten.Data.Entities;

namespace URLShorten.Data
{
    public class UrlShortenDbContext : DbContext
    {
        // ✅ Constructor nhận options từ dependency injection
        public UrlShortenDbContext(DbContextOptions<UrlShortenDbContext> options)
            : base(options)
        {
        }

        // ✅ Các bảng (DbSet)
        //public DbSet<User> Users { get; set; }
        public DbSet<UrlLink> UrlLinks { get; set; }

        // ✅ Cấu hình chi tiết quan hệ và ràng buộc
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🧩 Cấu hình bảng User
            //modelBuilder.Entity<User>(entity =>
            //{

            //    entity.HasKey(u => u.Id);

            //    entity.Property(u => u.Email)
            //          .IsRequired()
            //          .HasMaxLength(MaxLengths.Email);

            //    entity.Property(u => u.DisplayName)
            //          .IsRequired()
            //          .HasMaxLength(MaxLengths.DisplayName);

            //    entity.Property(u => u.PasswordHash)
            //          .IsRequired();

            //    entity.Property(u => u.Role)
            //          .HasMaxLength(MaxLengths.Role);


            //    entity.Property(u => u.IsActive);

            //});

            // 🧩 Cấu hình bảng UrlLink
            modelBuilder.Entity<UrlLink>(entity =>
            {
                entity.HasKey(l => l.Id);

                entity.Property(l => l.OriginalUrl)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.OriginalURL);

                entity.Property(l => l.ShortenedUrl)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.ShortenedURL);

                entity.Property(l => l.CustomAlias)
                      .HasMaxLength(MaxLengths.CustomAlias);


                entity.Property(l => l.ClickCount);


                entity.Property(l => l.IsActive);


                // 🔗 Một User có thể tạo nhiều UrlLink
                //entity.HasOne(l => l.User)
                //      .WithMany() // nếu bạn thêm ICollection<UrlLink> trong User thì thay bằng .WithMany(u => u.ShortUrls)
                //      .HasForeignKey(l => l.UserId)
                //      .OnDelete(DeleteBehavior.Restrict); // không xóa URL khi xóa user
            });
        }
    }
}
