using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace docs.host
{
    public class LocaleHelper
    {
        public static readonly ReadOnlyCollection<CultureInfo> SupportedCultures;
        public static readonly ImmutableHashSet<string> SupportedCulturesNames;

        private static readonly char[] PathSeparators = new[] { '/' };

        static LocaleHelper()
        {
            SupportedCultures = Array.AsReadOnly(CultureInfo.GetCultures(CultureTypes.AllCultures).ToArray());
            SupportedCulturesNames = ImmutableHashSet.Create(
                StringComparer.OrdinalIgnoreCase,
                SupportedCultures.Select(x => x.Name).ToArray());
        }

        public static bool IsValidLocale(string localeName)
        {
            return SupportedCulturesNames.Contains(localeName);
        }

        public static string GetPathWithoutLocale(string path, out string locale)
        {
            string firstSegment = path.Split(
                PathSeparators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if (firstSegment != null && IsValidLocale(firstSegment))
            {
                locale = firstSegment;
                return path.TrimStart('/').Substring(locale.Length);
            }

            locale = null;
            return path;
        }
    }
}
