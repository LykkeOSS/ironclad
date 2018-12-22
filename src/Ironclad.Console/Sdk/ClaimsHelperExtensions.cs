// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class ClaimsHelperExtensions
    {
        private const char ClaimValueSeparator = '=';

        public static KeyValuePair<string, object> ToKeyValuePair(this string src)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                throw new ArgumentNullException(nameof(src));
            }

            var split = src.Split(ClaimValueSeparator);

            return new KeyValuePair<string, object>(
                split.First(),
                split.Last());
        }

        public static IEnumerable<KeyValuePair<string, IEnumerable<object>>> ToClaims(
            this IEnumerable<KeyValuePair<string, object>> src)
        {
            return src
                .GroupBy(x => x.Key)
                .Select(g => new KeyValuePair<string, IEnumerable<object>>(
                    g.Key,
                    g.Select(x => x.Value).ToList()));
        }
    }
}