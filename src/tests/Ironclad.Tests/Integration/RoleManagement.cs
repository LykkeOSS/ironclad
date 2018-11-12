// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Feature
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class RoleManagement : IntegrationTest
    {
        public RoleManagement(AuthenticationFixture securityFixture, IroncladFixture ironcladFixture)
            : base(securityFixture, ironcladFixture)
        {
        }

        [Fact]
        public async Task CanAddRole()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // act
            await httpClient.AddRoleAsync(role).ConfigureAwait(false);

            // assert
            var roleExists = await httpClient.RoleExistsAsync(role).ConfigureAwait(false);
            roleExists.Should().BeTrue();
        }

        [Fact]
        public async Task CanGetRoleSummaries()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            await httpClient.AddRoleAsync(role).ConfigureAwait(false);

            // act
            var roles = await httpClient.GetRolesAsync().ConfigureAwait(false);

            // assert
            roles.Should().NotBeNull();
            roles.Should().Contain(role);
        }

        [Fact]
        public async Task CanGetRoleSummariesWithQuery()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role1 = "query";
            var role2 = "query_test_02";
            var role3 = "query_test_03";

            await httpClient.AddRoleAsync(role1).ConfigureAwait(false);
            await httpClient.AddRoleAsync(role2).ConfigureAwait(false);
            await httpClient.AddRoleAsync(role3).ConfigureAwait(false);

            // act
            var roles = await httpClient.GetRolesAsync("query_").ConfigureAwait(false);

            // assert
            roles.Should().NotBeNull();
            roles.Should().HaveCount(2);
            roles.Should().Contain(new[] { role2, role3 });
        }

        [Fact]
        public async Task CanRemoveRole()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            await httpClient.AddRoleAsync(role).ConfigureAwait(false);

            // act
            await httpClient.RemoveRoleAsync(role).ConfigureAwait(false);

            // assert
            var roles = await httpClient.GetRolesAsync().ConfigureAwait(false);
            roles.Should().NotBeNull();
            roles.Should().NotContain(role);
        }

        [Fact]
        public async Task CannotAddDuplicateRole()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            await httpClient.AddRoleAsync(role).ConfigureAwait(false);

            // act
            Func<Task> func = async () => await httpClient.AddRoleAsync(role).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public void CannotRemoveAdminRole()
        {
            // arrange
            var httpClient = new RolesHttpClient(this.Authority, this.Handler);
            var role = "admin";

            // act
            Func<Task> func = async () => await httpClient.RemoveRoleAsync(role).ConfigureAwait(false);

            // assert
            func.Should().Throw<HttpException>().And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
