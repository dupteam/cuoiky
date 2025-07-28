using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebLuuFile.Models;

namespace WebLuuFile.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileModel> Files { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }
        public DbSet<DownloadLog> DownloadLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình khóa ngoại cho FileVersion
            modelBuilder.Entity<FileVersion>()
                .HasOne(fv => fv.FileModel)
                .WithMany(f => f.FileVersions)
                .HasForeignKey(fv => fv.FileId);

            // Cấu hình Cascade Delete cho DownloadLogs khi xoá File
            modelBuilder.Entity<DownloadLog>()
                .HasOne(dl => dl.File)
                .WithMany(f => f.DownloadLogs)
                .HasForeignKey(dl => dl.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
