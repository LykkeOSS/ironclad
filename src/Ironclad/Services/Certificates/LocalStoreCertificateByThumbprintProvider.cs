// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class LocalStoreCertificateByThumbprintProvider : ICertificateProvider
    {
        private readonly string thumbprint;
        private readonly ILogger<LocalStoreCertificateByThumbprintProvider> logger;

        public LocalStoreCertificateByThumbprintProvider(string thumbprint, ILogger<LocalStoreCertificateByThumbprintProvider> logger)
        {
            this.thumbprint = thumbprint;
            this.logger = logger;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<X509Certificate2> GetCertificateAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            this.logger.LogInformation($"Loading {this.thumbprint} certificate from local store.");
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, this.thumbprint, false);
                store.Close();

                if (certs.Count == 0)
                {
                    this.logger.LogError($"Certificate {this.thumbprint} not found in local store.");
                    throw new InvalidOperationException($"Certificate {this.thumbprint} not found in local store.");
                }

                return certs[0];
            }
        }
    }
}
