// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace DbContextExtensions.Mappings
{
    /// <summary>
    /// Implements Entity Framework Core Type Configurations using the 
    /// <see cref="ModelBuilder"/>. This class is used as an abstraction, 
    /// so we can pass the <see cref="IEntityTypeConfiguration{TEntity}"/> 
    /// into a <see cref="DbContext"/>.
    /// </summary>
    public interface IEntityMap
    {
        /// <summary>
        /// Configures the <see cref="ModelBuilder"/> for an entity.
        /// </summary>
        /// <param name="builder"><see cref="ModelBuilder"/></param>
        void Map(ModelBuilder builder);
    }
}