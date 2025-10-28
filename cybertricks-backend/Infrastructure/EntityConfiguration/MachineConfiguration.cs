using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ct.backend.Infrastructure.EntityConfiguration
{
    public class MachineConfiguration : IEntityTypeConfiguration<Machine>
    {
        public void Configure(EntityTypeBuilder<Machine> b)
        {
            b.ToTable("Machines");
            b.HasKey(x => x.MachineId);

            b.Property(x => x.Code).HasMaxLength(50).IsRequired();
            //b.Property(x => x.Status).AsStringEnum().HasMaxLength(20)
            // .HasDefaultValue(MachineStatus.available);

            // JSON spec (MySQL có thể set JSON nếu muốn)
            b.Property(x => x.SpecJson).HasColumnType("json"); // hoặc longtext

            b.HasOne(x => x.Room)
             .WithMany(r => r.Machines)
             .HasForeignKey(x => x.RoomId)
             .OnDelete(DeleteBehavior.Cascade);

            // Unique code theo cửa phòng (tuỳ business)
            b.HasIndex(x => new { x.RoomId, x.Code }).IsUnique();

            b.ConfigureTimestamps();
        }
    }
}
