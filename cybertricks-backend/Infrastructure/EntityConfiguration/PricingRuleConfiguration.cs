using ct.backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ct.backend.Infrastructure.EntityConfiguration;

public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> b)
    {
        b.ToTable("PricingRules");

        b.HasKey(x => x.PricingRuleId);

        // FK -> Store
        b.HasOne(x => x.Store)
         .WithMany()                 // nếu Store có ICollection<PricingRule> PricingRules thì đổi thành .WithMany(s => s.PricingRules)
         .HasForeignKey(x => x.StoreId)
         .OnDelete(DeleteBehavior.Cascade);

        // Enum mapping (string)
        b.Property(x => x.Status).AsStringEnum();     // PricingStatus
        b.Property(x => x.RoomType).AsStringEnum();   // RoomType? (nullable)

        // Giá & hệ số
        b.Property(x => x.BasePricePerHour)
         .HasColumnType("decimal(10,2)")
         .IsRequired();

        b.Property(x => x.HourlyMultiplier)
         .HasColumnType("decimal(5,2)"); // ví dụ: 1.25, 1.50

        // Khung giờ
        b.Property(x => x.StartHour).HasColumnType("tinyint unsigned"); // 0..23
        b.Property(x => x.EndHour).HasColumnType("tinyint unsigned");   // 0..23

        // Ngày trong tuần (nếu bạn vẫn dùng string)
        b.Property(x => x.DayOfWeek)
         .HasMaxLength(20);

        // Mô tả
        b.Property(x => x.Description)
         .HasMaxLength(500);

        // Timestamps
        b.ConfigureTimestamps();

        // (Tuỳ chọn) Index giúp query rule nhanh theo Store + khung giờ + ngày
        b.HasIndex(x => new { x.StoreId, x.RoomType, x.DayOfWeek, x.StartHour, x.EndHour, x.Status });
    }
}