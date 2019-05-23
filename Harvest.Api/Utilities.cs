using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Harvest.Api
{
    public class Utilities
    {
        private static readonly Regex scopeRegex = new Regex("harvest:(?<harvestid>[^ ]*)");

        internal static Dictionary<string, string> ParseQueryString(string query)
        {
            return query.Split('&').Select(x => x.Split('='))
                .ToDictionary(x => Uri.UnescapeDataString(x[0]), y => y.Length > 1 ? Uri.UnescapeDataString(y[1]) : null);
        }

        public static string GenerateState()
        {
            using (var random = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[32];
                random.GetBytes(data);
                return Convert.ToBase64String(data);
            }
        }

        public static long? FirstHarvestAccountId(string scope)
        {
            var accounts = ParseHarvestAccounts(scope);
            return accounts.Count > 0 && long.TryParse(accounts[0], out var id) ? (long?)id : null;
        }

        private static List<string> ParseHarvestAccounts(string scope)
        {
            var result = new List<string>();
            var mathes = scopeRegex.Matches(scope);

            foreach (Match match in mathes)
            {
                if (match.Success)
                    result.Add(match.Groups["harvestid"].Value);
            }

            return result;
        }

    }
}
