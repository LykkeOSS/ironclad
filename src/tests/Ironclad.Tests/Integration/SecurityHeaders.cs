// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class SecurityHeaders : IntegrationTest
    {
        public SecurityHeaders(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture)
            : base(securityFixture, ironcladFixture)
        {
        }

        [Theory]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "SAMEORIGIN")]
        [InlineData("Referrer-Policy", "no-referrer")]
        [InlineData("X-XSS-Protection", "1; mode=block")]
        [InlineData("Content-Security-Policy", "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';")]
        [InlineData("X-Content-Security-Policy", "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';")]
        [InlineData("Feature-Policy", "geolocation 'none'; midi 'none'; notifications 'self'; push 'self'; microphone 'none'; camera 'none'; magnetometer 'none'; gyroscope 'none'; speaker 'none'; vibrate 'none'; payment 'none'")]
        public async Task ShouldConformToWwwSecurityHeadersIO(string header, string expectedValue)
        {
            /* https://securityheaders.io */

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, this.Authority))
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                response.Headers.GetValues(header).Should().Contain(expectedValue);
            }
        }
    }
}
