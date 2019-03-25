// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Bug
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class Bug00090 : AuthenticationTest
    {
        public Bug00090(AuthenticationFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void ShouldNotThrowInternalServerError()
        {
            // arrange
            var httpClient = new UsersHttpClient(this.ApiUri, this.Handler);
            var user = new User
            {
                Username = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                Password = "pas$wrod",
                Email = "test@test.com",
                PhoneNumber = "0123456789",
                Roles = { "auth_admin", "user_admin" },
            };

            // act
            Func<Task> func = async () => await httpClient.AddUserAsync(user).ConfigureAwait(false);

            // assert
            func.Should().NotThrow<HttpException>();
        }
    }
}
