// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Mappings;
using DbContextExtensions.Test.Example.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DbContextExtensions.Test.Example.Database
{
    public class HeroEntityMap : EntityMap<Hero>
    {
        protected override void InternalMap(ModelBuilder model, EntityTypeBuilder<Hero> entity)
        {
            model
                .HasSequence("SeqHero", seq_builder => seq_builder.IncrementsBy(10));

            entity
                .HasKey(x => x.Id);

            entity
                .Property(x => x.Id)
                .UseHiLo("SeqHero")
                .HasColumnName("HeroID");

            entity
                .Property(x => x.Name)
                .HasColumnName("Name")
                .IsRequired();

            entity
                .HasMany(x => x.Superpowers)
                .WithMany(x => x.Heroes)
                .UsingEntity<HeroSuperpower>(
                    configureLeft: j => j.HasOne(x => x.Hero).WithMany().HasForeignKey(x => x.HeroId),
                    configureRight: j => j.HasOne(x => x.Superpower).WithMany().HasForeignKey(x => x.SuperpowerId));
        }
    }
}
