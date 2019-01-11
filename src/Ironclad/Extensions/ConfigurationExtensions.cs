// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Extensions
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;

    internal static class ConfigurationExtensions
    {
        internal static IConfigurationBuilder AddAzureKeyVaultFromConfig(this IConfigurationBuilder builder, string[] args)
        {
            const string key = "azure:key_vault";

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var settings = configuration.GetSection("azure:key_vault")?.Get<Settings.AzureSettings.KeyVaultSettings>(options => options.BindNonPublicProperties = true);
            if (settings == null)
            {
                return builder;
            }

            if (settings?.GetValidationErrors().Any() == true)
            {
                var stringBuilder = new StringBuilder();
                var errors = settings.GetValidationErrors().Select(value => string.Format(CultureInfo.InvariantCulture, value, key));
                stringBuilder.Append($"\r\nErrors in '{key}' section:\r\n - {string.Join("\r\n - ", errors)}");

                // TODO (Cameron): Change link to point to somewhere sensible (when it exists).
                throw new InvalidOperationException(
                    $@"Validation of configuration settings failed.{stringBuilder.ToString()}
Please see https://gist.github.com/cameronfletcher/58673a468c8ebbbf91b81e706063ba56 for more information.");
            }

            return builder.AddAzureKeyVault(settings.Endpoint, settings.Client, new UnderscoreKeyVaultSecretManager());
        }

        internal class UnderscoreKeyVaultSecretManager : IKeyVaultSecretManager
        {
            public bool Load(SecretItem secret)
            {
                // Load all
                return true;
            }

            public string GetKey(SecretBundle secret)
            {
                // Replace one dash in any name with an underscore and replace two
                // dashes in any name with the KeyDelimiter, which is the
                // delimiter used in configuration (usually a colon). Azure
                // Key Vault doesn't allow a colon in secret names or an underscore.
                return secret.SecretIdentifier.Name
                    .Replace("--", ConfigurationPath.KeyDelimiter, StringComparison.OrdinalIgnoreCase)
                    .Replace("-", "_", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
