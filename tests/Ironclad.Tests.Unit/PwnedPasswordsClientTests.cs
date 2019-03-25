// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Unit
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Flurl.Http.Testing;
    using Services.Passwords;
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using Xunit;

    public class PwnedPasswordsClientTests : IDisposable
    {
        private readonly ILogger<PwnedPasswordsClient> _fakeLogger;
        private readonly HttpTest _httpTest;
        private const string TestPassword = "TestPassword";
        private const string Sha1Suffix = "25b226df62870ae23af8d3fac0760d71588";

        public PwnedPasswordsClientTests()
        {
            _fakeLogger = Substitute.For<ILogger<PwnedPasswordsClient>>();
            _httpTest = new HttpTest();
        }

        [Fact]
        public async Task HasPasswordBeenPwned_ClientReturnsNot200StatusCode_ReturnsFalse()
        {
            using (var fakeHttpClient = new HttpClient(new FakeHttpMessageHandler()))
            {
                fakeHttpClient.BaseAddress = new Uri("http://localhost");
                _httpTest.RespondWith("", 404);

                var service = new PwnedPasswordsClient(_fakeLogger, fakeHttpClient);

                var isPwned = await service.HasPasswordBeenPwnedAsync(TestPassword);

                isPwned.Should().BeFalse();
            }
        }

        [Fact]
        public async Task HasPasswordBeenPwned_ExceptionThrown_ReturnsFalse()
        {
            using (var fakeHttpClient = new HttpClient(new ThrowExceptionMessageHandler()))
            {
                fakeHttpClient.BaseAddress = new Uri("http://localhost");

                var service = new PwnedPasswordsClient(_fakeLogger, fakeHttpClient);

                var isPwned = await service.HasPasswordBeenPwnedAsync(TestPassword);

                isPwned.Should().BeFalse();
            }
        }

        [Fact]
        public async Task HasPasswordBeenPwned_PasswordPwnd_ReturnsTrue()
        {
            using (var fakeHttpClient = new HttpClient(new FakeHttpMessageHandler()))
            {
                fakeHttpClient.BaseAddress = new Uri("http://localhost");
                _httpTest.RespondWith($"{Sha1Suffix}:250", 200);

                var service = new PwnedPasswordsClient(_fakeLogger, fakeHttpClient);

                var isPwned = await service.HasPasswordBeenPwnedAsync(TestPassword);

                isPwned.Should().BeTrue();
            }
        }

        [Fact]
        public async Task HasPasswordBeenPwned_PasswordIsEmpty_ReturnsFalse()
        {
            using (var fakeHttpClient = new HttpClient(new FakeHttpMessageHandler()))
            {
                fakeHttpClient.BaseAddress = new Uri("http://localhost");
                _httpTest.RespondWith($"{Sha1Suffix}:250", 200);

                var service = new PwnedPasswordsClient(_fakeLogger, fakeHttpClient);

                var isPwned = await service.HasPasswordBeenPwnedAsync("");

                isPwned.Should().BeFalse();
            }
        }

        [Fact]
        public async Task HasPasswordBeenPwned_PasswordIsNull_ReturnsFalse()
        {
            using (var fakeHttpClient = new HttpClient(new FakeHttpMessageHandler()))
            {
                fakeHttpClient.BaseAddress = new Uri("http://localhost");
                _httpTest.RespondWith($"{Sha1Suffix}:250", 200);

                var service = new PwnedPasswordsClient(_fakeLogger, fakeHttpClient);

                var isPwned = await service.HasPasswordBeenPwnedAsync(null);

                isPwned.Should().BeFalse();
            }
        }

        public void Dispose()
        {
            _httpTest.Dispose();
        }
    }
}
