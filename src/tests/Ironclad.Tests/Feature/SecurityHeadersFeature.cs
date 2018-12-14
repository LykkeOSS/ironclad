// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class SecurityHeadersFeature : AuthenticationTest
    {
        public SecurityHeadersFeature(AuthenticationFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "SAMEORIGIN")]
        [InlineData("Referrer-Policy", "no-referrer")]
        [InlineData("X-XSS-Protection", "1; mode=block")]
        [InlineData("Content-Security-Policy", "script-src 'self' 'sha256-SjXRkVC/0M0+WLq2GU4E8JdbZ/ZNgspoHSzWQaMhG7E=' 'sha256-nEhC/Gar4FM2L9jcHtSP+DYaxFlJNy7jG8KD46S2SlI=' 'sha256-od6N/tEDLReTkDWYLIQ4wfOY9HAR4vm2mqgP2a8XdTU=' 'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE=' 'sha256-RBBmlHImRT323C+VE9PFBMXnHbki8sYW3t1e6JhINnU=' https://az416426.vo.msecnd.net/scripts/a/ai.0.js https://ajax.aspnetcdn.com/ajax/jquery.validate/1.14.0/jquery.validate.min.js https://ajax.aspnetcdn.com/ajax/jquery.validation.unobtrusive/3.2.6/jquery.validate.unobtrusive.min.js; img-src 'self' data:; style-src 'self' 'unsafe-inline'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';")]
        [InlineData("X-Content-Security-Policy", "script-src 'self' 'sha256-SjXRkVC/0M0+WLq2GU4E8JdbZ/ZNgspoHSzWQaMhG7E=' 'sha256-nEhC/Gar4FM2L9jcHtSP+DYaxFlJNy7jG8KD46S2SlI=' 'sha256-od6N/tEDLReTkDWYLIQ4wfOY9HAR4vm2mqgP2a8XdTU=' 'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE=' 'sha256-RBBmlHImRT323C+VE9PFBMXnHbki8sYW3t1e6JhINnU=' https://az416426.vo.msecnd.net/scripts/a/ai.0.js https://ajax.aspnetcdn.com/ajax/jquery.validate/1.14.0/jquery.validate.min.js https://ajax.aspnetcdn.com/ajax/jquery.validation.unobtrusive/3.2.6/jquery.validate.unobtrusive.min.js; img-src 'self' data:; style-src 'self' 'unsafe-inline'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';")]
        [InlineData("Feature-Policy", "geolocation 'none'; microphone 'none'; camera 'none'")]
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
