// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class CertificateProviderFactory
    {
        private readonly CertificateConfiguration config;
        private readonly IServiceProvider serviceProvider;

        public CertificateProviderFactory(CertificateConfiguration config, IServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        public ICertificateProvider GetProvider()
        {
            if (!string.IsNullOrEmpty(this.config.AzureFileStorageConnectionString))
            {
                return new AzureFileStorageCertificateProvider(
                    this.config.AzureFileStorageConnectionString,
                    this.config.AzureFileStorageShareName,
                    this.config.CertificatePath,
                    this.serviceProvider.GetService<ILogger<AzureFileStorageCertificateProvider>>());
            }

            if (!string.IsNullOrEmpty(this.config.CertificateThumbprint))
            {
                return new LocalStoreCertificateByThumbprintProvider(
                    this.config.CertificateThumbprint,
                    this.serviceProvider.GetService<ILogger<LocalStoreCertificateByThumbprintProvider>>());
            }

            if (!string.IsNullOrEmpty(this.config.CertificatePath))
            {
                return new FileCertificateProvider(
                    this.config.CertificatePath,
                    this.serviceProvider.GetService<ILogger<FileCertificateProvider>>());
            }

            return null;
        }
    }
}
