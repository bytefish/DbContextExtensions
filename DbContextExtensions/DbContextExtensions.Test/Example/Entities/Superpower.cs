// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DbContextExtensions.Test.Example.Entities
{
    public class Superpower
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
        /// Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Heroes for this Superpower.
        /// </summary>
        public ICollection<Hero> Heroes { get; set; }

        /// <summary>
        /// Returns a Superpower as a String.
        /// </summary>
        /// <returns>String Representation for a Superpower</returns>
        public override string ToString()
        {
            return $"Superpower (Id={Id}, Name={Name})";
        }
    }
}
