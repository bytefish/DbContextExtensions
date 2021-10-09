// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Mappings;
using DbContextExtensions.Test.Example.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DbContextExtensions.Test.Example.Database
{
    public class HeroSuperpowerEntityMap : EntityMap<HeroSuperpower>
    {
        protected override void InternalMap(ModelBuilder model, EntityTypeBuilder<HeroSuperpower> entity)
        {
            entity
                .HasKey(x => new { x.HeroId, x.SuperpowerId });

            entity
                .Property(x => x.HeroId);

            entity
                .HasOne(x => x.Hero)
                .WithMany()
                .HasForeignKey(x => x.HeroId)
                .HasConstraintName("FK_HeroSuperpower_Hero");

            entity
                .HasOne(x => x.Superpower)
                .WithMany()
                .HasForeignKey(x => x.SuperpowerId)
                .HasConstraintName("FK_HeroSuperpower_Superpower");
        }
    }
}
