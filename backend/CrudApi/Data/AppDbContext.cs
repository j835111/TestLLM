using CrudApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(120).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(1000);
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
            entity.Property(product => product.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity.Property(product => product.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity.HasIndex(product => product.Name);
        });
    }
}
