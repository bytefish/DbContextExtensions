using System.Collections.Generic;
using System.Linq;

namespace DbContextExtensions.Test.Utils
{
    public static class StringUtils
    {
        public static string ListToString<T>(ICollection<T> collection)
        {
            if(collection == null)
            {
                return null;
            }

            var elementsAsString = collection
                .Select(x => x.ToString())
                .ToArray();

            return string.Join(", ", elementsAsString);
        }
    }
}
