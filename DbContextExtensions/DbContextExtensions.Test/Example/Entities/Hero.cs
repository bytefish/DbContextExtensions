// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DbContextExtensions.Test.Utils;
using System.Collections.Generic;

namespace DbContextExtensions.Test.Example.Entities
{
    /// <summary>
    /// A Hero.
    /// </summary>
    public class Hero
    {
        /// <summary>
        /// Primary Key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Superpowers.
        /// </summary>
        public ICollection<Superpower> Superpowers { get; set; }

        /// <summary>
        /// Returns a Hero as a String.
        /// </summary>
        /// <returns>String Representation for a Hero</returns>
        public override string ToString()
        {
            return $"Hero (Id={Id}, Name={Name}, Superpowers=[{StringUtils.ListToString(Superpowers)}])";
        }
    }
}
