using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    public static class Reader
    {
        private const string DefaultLocale = "en-us";
        private const string DefaultFallbackBranch = "master";
        private const string DefaultBranch = "live";

        private static readonly Dictionary<string, string> LocaleFallbackNonEnUsRules =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "de-at", "de-de" },
                { "de-ch", "de-de" },
                { "es-mx", "es-es" },
                { "fr-be", "fr-fr" },
                { "fr-ca", "fr-fr" },
                { "fr-ch", "fr-fr" },
                { "it-ch", "it-it" },
                { "kk-kz", "ru-ru" },
                { "nl-be", "nl-nl" },
                { "zh-hk", "zh-tw" }
            };

        public static async Task<Document> QueryDocument(string url, string branch, string locale, string moniker)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            // pr branch could fall back
            HashSet<string> branches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(branch))
            {
                branch = DefaultBranch;
            }
            branches.Add(branch);
            if (branch.StartsWith("pr-", StringComparison.OrdinalIgnoreCase))
            {
                branches.Add(DefaultFallbackBranch);
            }

            // non en-us locale could fall back
            HashSet<string> locales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(locale))
            {
                locale = DefaultLocale;
            }
            locales.Add(locale);
            if (LocaleFallbackNonEnUsRules.ContainsKey(locale))
            {
                locales.Add(LocaleFallbackNonEnUsRules[locale]);
            }

            //normalize moniker
            if (string.IsNullOrWhiteSpace(moniker))
            {
                moniker = null;
            }

            // get all potential documents
            var docs = await CosmosDBAccessor<Document>.QueryAsync(
                doc =>
                doc.Url == url.ToLowerInvariant() &&
                branches.Contains(doc.Branch) &&
                locales.Contains(doc.Locale));
            if (!docs.Any())
            {
                return null;
            }

            // get all actives
            HashSet<string> activeEtags = new HashSet<string>(docs.Select(doc => doc.ActiveEtag));
            var actives = await CosmosDBAccessor<Active>.QueryAsync(active => activeEtags.Contains(active.ActiveEtag));
            activeEtags = new HashSet<string>(actives.Select(active => active.ActiveEtag));
            if (!activeEtags.Any())
            {
                return null;
            }

            // best match
            Document result = null;
            int bestMatch = -1;
            string finalMoniker = null;
            foreach (Document doc in docs.Where(d => activeEtags.Contains(d.ActiveEtag)))
            {
                // branch fallback
                int score = string.Equals(branch, doc.Branch, StringComparison.OrdinalIgnoreCase) ? 200000 : 100000;

                // moniker fallback
                string matchedMoniker = null;
                if (doc.Monikers != null && doc.Monikers.Contains(moniker, StringComparer.OrdinalIgnoreCase))
                {
                    score += 2000;
                    matchedMoniker = moniker;
                }
                else
                {
                    score += 1000;
                    matchedMoniker = doc.Monikers?.FirstOrDefault();
                }

                // locale fallback
                if (string.Equals(locale, doc.Locale, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                }
                else if (LocaleFallbackNonEnUsRules.TryGetValue(locale, out string fallbackLocale) && string.Equals(fallbackLocale, doc.Locale, StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                }
                else
                {
                    score += 10;
                }

                if (score > bestMatch)
                {
                    result = doc;
                    bestMatch = score;
                    finalMoniker = matchedMoniker;
                }
            }

            result.Monikers = new List<string> { finalMoniker };
            return result;
        }
    }
}
