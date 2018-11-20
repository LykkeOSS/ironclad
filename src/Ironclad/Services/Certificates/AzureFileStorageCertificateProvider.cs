// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.File;

    public class AzureFileStorageCertificateProvider : ICertificateProvider
    {
        private readonly string connectionString;
        private readonly string shareName;
        private readonly string fileName;
        private readonly ILogger<AzureFileStorageCertificateProvider> logger;

        public AzureFileStorageCertificateProvider(string connectionString, string shareName, string fileName, ILogger<AzureFileStorageCertificateProvider> logger)
        {
            this.connectionString = connectionString;
            this.shareName = shareName;
            this.fileName = fileName;
            this.logger = logger;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            this.logger.LogInformation($"Loading {this.fileName} certificate from Azure Storage.");
            byte[] file = await this.GetFile().ConfigureAwait(false);
            var cert = new X509Certificate2(file);

            return cert;
        }

        private async Task<byte[]> GetFile()
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this.connectionString);

            // Create a CloudFileClient object for credentialed access to Azure Files.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

            // Get a reference to the file share we created previously.
            CloudFileShare share = fileClient.GetShareReference(this.shareName);

            // Ensure that the share exists.
            if (await share.ExistsAsync().ConfigureAwait(false))
            {
                // Get a reference to the root directory for the share.
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();

                // Get a reference to the file we created previously.
                CloudFile file = rootDir.GetFileReference(this.fileName);

                // Ensure that the file exists.
                if (await file.ExistsAsync().ConfigureAwait(false))
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.DownloadToStreamAsync(stream).ConfigureAwait(false);

                        return stream.ToArray();
                    }
                }
            }

            this.logger.LogError($"Certificate file {this.fileName} not found on Azure storage share {this.shareName}.");
            throw new FileNotFoundException($"Certificate file {this.fileName} not found on Azure storage share {this.shareName}.");
        }
    }
}
