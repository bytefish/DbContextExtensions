// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Mappings;
using DbContextExtensions.Test.Example.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DbContextExtensions.Test.Example.Database
{
    public class SuperpowerEntityMap : EntityMap<Superpower>
    {
        protected override void InternalMap(ModelBuilder model, EntityTypeBuilder<Superpower> entity)
        {
            model
                .HasSequence("SeqSuperpower", seq_builder => seq_builder.IncrementsBy(10));

            entity
                .HasKey(x => x.Id);

            entity
                .Property(x => x.Id)
                .UseHiLo("SeqSuperpower")
                .HasColumnName("SuperpowerID");

            entity
                .Property(x => x.Name)
                .HasColumnName("Name")
                .IsRequired();

            entity
                .Property(x => x.Description)
                .HasColumnName("Description");

            entity
                .HasMany(x => x.Heroes)
                .WithMany(x => x.Superpowers)
                .UsingEntity<HeroSuperpower>(
                    configureLeft: j => j.HasOne(x => x.Superpower).WithMany().HasForeignKey(x => x.SuperpowerId),
                    configureRight: j => j.HasOne(x => x.Hero).WithMany().HasForeignKey(x => x.HeroId));
        }
    }
}
