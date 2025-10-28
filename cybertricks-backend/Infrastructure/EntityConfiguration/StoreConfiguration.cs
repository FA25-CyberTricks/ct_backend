using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ct.backend.Infrastructure.EntityConfiguration
{
    public class StoreConfiguration : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> b)
        {
            b.ToTable("Stores");
            b.HasKey(x => x.StoreId);

            b.Property(x => x.BrandId).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.ContactPhone).HasMaxLength(50);
            b.Property(x => x.Status).AsStringEnum().HasMaxLength(20)
             .HasDefaultValue(StoreStatus.active);
            b.Property(x => x.DisplayOrder).HasDefaultValue(0);

            b.HasIndex(x => new { x.BrandId, x.Name }); // tìm kiếm nhanh theo brand + tên
                                                        // Cấu hình Latitude
            b.Property(s => s.Latitude)
                .HasColumnType("decimal(12,9)")   // kiểu DECIMAL(9,6) trong MySQL
                .IsRequired(false);              // cho phép null

            b.Property(s => s.Longitude)
                .HasColumnType("decimal(12,9)")   // kiểu DECIMAL(9,6)
                .IsRequired(false);              // cho phép null

            b.HasMany(x => x.Floors)
             .WithOne(f => f.Store)
             .HasForeignKey(f => f.StoreId)
             .OnDelete(DeleteBehavior.Cascade);

            b.ConfigureTimestamps();
        }
    }
}
