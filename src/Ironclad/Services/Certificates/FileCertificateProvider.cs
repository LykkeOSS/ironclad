// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class FileCertificateProvider : ICertificateProvider
    {
        private readonly string filePath;
        private readonly ILogger<FileCertificateProvider> logger;

        public FileCertificateProvider(string filePath, ILogger<FileCertificateProvider> logger)
        {
            this.filePath = filePath;
            this.logger = logger;
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            if (!File.Exists(this.filePath))
            {
                this.logger.LogError($"Certificate file {this.filePath} not found.");
                throw new FileNotFoundException($"Certificate file {this.filePath} not found.");
            }

            this.logger.LogInformation($"Loading certificate from {this.filePath} path.");
            var file = await File.ReadAllBytesAsync(this.filePath).ConfigureAwait(false);

            return new X509Certificate2(file);
        }
    }
}
