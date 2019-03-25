namespace SampleTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using IdentityModel.Client;
    using Ironclad.Client;
    using Ironclad.Tests.Sdk;
    using Xunit;

    public class SampleTest : AuthenticationTest
    {
        private readonly Client client;

        public SampleTest(SampleAuthenticationFixture fixture)
            : base(fixture)
        {
            this.client = fixture.Client;
        }

        // TODO (Cameron): Uncomment the attribute below to execute this test. This should not be executed as part of any normal build Ironclad - or discovered.
        ////[Fact]
        public async Task SampleAuthenticationTest()
        {
            // arrange
            var user = new User
            {
                Username = "johnsmith",
                Password = "pas$wrod",
            };

            var usersClient = new UsersHttpClient(this.ApiUri, this.Handler);
            await usersClient.AddUserAsync(user);

            // act
            var url = new RequestUrl(this.Authority + "/connect/authorize")
                .CreateAuthorizeUrl(this.client.Id, "id_token token", "openid profile sample_api", this.client.RedirectUris.First(), "state", "nonce");

            var automation = new BrowserAutomation(user.Username, user.Password);
            await automation.NavigateToLoginAsync(url).ConfigureAwait(false);
            var response = await automation.LoginToAuthorizationServerAndCaptureRedirectAsync().ConfigureAwait(false);

            // assert
            Assert.False(response.IsError);
            Assert.NotNull(response.AccessToken);
        }
    }
}
