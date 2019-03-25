// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA5350

namespace Ironclad.Services.Passwords
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <inheritdoc />
    public class PwnedPasswordsClient : IPwnedPasswordsClient
    {
        public const string HttpClientName = nameof(PwnedPasswordsClient);
        private static readonly SHA1 Sha1 = SHA1.Create();
        private readonly HttpClient client;
        private readonly ILogger<PwnedPasswordsClient> logger;

        public PwnedPasswordsClient(
            ILogger<PwnedPasswordsClient> logger,
            HttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async Task<bool> HasPasswordBeenPwnedAsync(string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(password))
            {
                this.logger.LogWarning("Password is empty, pwned check is skipped.");
                return false;
            }

            if (this.client.BaseAddress == null)
            {
                this.logger.LogWarning("Pwned passwords check is disabled, because URI for API not set up.");
                return false;
            }

            var sha1Password = Sha1HashStringForUtf8String(password);
            var sha1Prefix = sha1Password.Substring(0, 5);
            var sha1Suffix = sha1Password.Substring(5);

            try
            {
#pragma warning disable CA2234
                var response = await this.client.GetAsync("range/" + sha1Prefix, cancellationToken);
#pragma warning restore CA2234

                if (response.IsSuccessStatusCode)
                {
                    // Response was a success. Check to see if the SAH1 suffix is in the response body.
                    var frequency = await ExtractFrequency(response.Content, sha1Suffix);
                    var isPwned = frequency > 0;
                    return isPwned;
                }

                this.logger.LogWarning($"Error calling Pwned Password API. Unexepected response from API: {response.StatusCode}. Assuming password is NOT pwned!");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error calling Pwned Password API. Assuming password is NOT pwned!");
            }

            return false;
        }

        /// <summary>
        ///     Compute hash for string
        /// </summary>
        /// <param name="s">String to be hashed</param>
        /// <returns>40-character hex string</returns>
        private static string Sha1HashStringForUtf8String(string s)
        {
            var hash = Sha1.ComputeHash(Encoding.UTF8.GetBytes(s));

            return string.Concat(hash.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        /// <summary>
        ///     Extract frequence from response.
        /// </summary>
        /// <param name="content">PwnedPasswords response context.</param>
        /// <param name="sha1Suffix">Suffix of password to check.</param>
        /// <returns>Number of times password was compromised.</returns>
        private static async Task<long> ExtractFrequency(HttpContent content, string sha1Suffix)
        {
            using (var streamReader = new StreamReader(await content.ReadAsStreamAsync()))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    var segments = line.Split(':');
                    if (segments.Length == 2
                        && string.Equals(segments[0], sha1Suffix, StringComparison.OrdinalIgnoreCase)
                        && long.TryParse(segments[1], out var count))
                    {
                        return count;
                    }
                }
            }

            return 0;
        }
    }
}
