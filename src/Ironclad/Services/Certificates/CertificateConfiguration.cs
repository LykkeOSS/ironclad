// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System;

    public class CertificateConfiguration
    {
        public CertificateConfiguration(
            string azureFileStorageConnectionString,
            string azureFileStorageShareName,
            string certificatePath,
            string certificateThumbprint)
        {
            if (
                (string.IsNullOrEmpty(azureFileStorageConnectionString) || string.IsNullOrEmpty(azureFileStorageShareName))
                && string.IsNullOrEmpty(certificatePath)
                && string.IsNullOrEmpty(certificateThumbprint))
            {
                throw new InvalidOperationException($"Certificate configuration is missing.");
            }

            this.AzureFileStorageConnectionString = azureFileStorageConnectionString;
            this.AzureFileStorageShareName = azureFileStorageShareName;
            this.CertificatePath = certificatePath;
            this.CertificateThumbprint = certificateThumbprint;
        }

        public string AzureFileStorageConnectionString { get; }

        public string AzureFileStorageShareName { get; }

        public string CertificatePath { get; }

        public string CertificateThumbprint { get; }
    }
}
