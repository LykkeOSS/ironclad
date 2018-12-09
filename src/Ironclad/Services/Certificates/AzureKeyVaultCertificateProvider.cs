// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Logging;
    using static Ironclad.Settings.AzureSettings;

    internal class AzureKeyVaultCertificateProvider : ICertificateProvider
    {
        private readonly KeyVaultSettings settings;
        private readonly string certificateId;
        private readonly ILogger logger;

        public AzureKeyVaultCertificateProvider(KeyVaultSettings settings, string certificateId, ILogger logger)
        {
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                throw new InvalidOperationException("Key vault connection string is missing in configuration.");
            }

            this.settings = settings;
            this.certificateId = certificateId;
            this.logger = logger;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            this.logger.LogInformation($"Loading {this.certificateId} from Azure key vault.");

            var tokenProvider = new AzureServiceTokenProvider(this.settings.ConnectionString);
            using (var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback)))
            {
                var cert = await keyVaultClient.GetCertificateAsync(this.certificateId).ConfigureAwait(false);

                if (cert == null)
                {
                    this.logger.LogError($"Key vault certificate {this.certificateId} not found.");
                    throw new InvalidOperationException($"Key vault certificate {this.certificateId} not found.");
                }

                var x509 = new X509Certificate2(cert.Cer);

                return x509;
            }
        }
    }
}
