using DMS.Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<Notification> Notification { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            builder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey("UserId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<DocumentVersion>()
                .HasOne(v => v.Document)
                .WithMany()
                .HasForeignKey(v => v.DocumentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
