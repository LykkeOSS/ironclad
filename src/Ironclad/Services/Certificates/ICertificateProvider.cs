// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Services.Certificates
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public interface ICertificateProvider
    {
        Task<X509Certificate2> GetCertificateAsync();
    }
}
