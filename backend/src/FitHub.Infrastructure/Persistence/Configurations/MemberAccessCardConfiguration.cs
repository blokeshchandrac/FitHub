using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FitHub.Infrastructure.Persistence.Configurations;
public sealed class MemberAccessCardConfiguration : EntityConfiguration<MemberAccessCard>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MemberAccessCard> builder)
    {
        builder.Property(x => x.UidHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.UidHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
