// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DbContextExtensions.Test.Example.Entities
{
    /// <summary>
    /// Association Table for a Hero and a Superpower.
    /// </summary>
    public class HeroSuperpower
    {
        /// <summary>
        /// The Hero.
        /// </summary>
        public Hero Hero { get; set; }

        /// <summary>
        /// The Superpower.
        /// </summary>
        public Superpower Superpower { get; set; }

        /// <summary>
        /// Hero FK reference.
        /// </summary>
        public int HeroId { get; set; }

        /// <summary>
        /// Superpower FK reference.
        /// </summary>
        public int SuperpowerId { get; set; }
    }
}
