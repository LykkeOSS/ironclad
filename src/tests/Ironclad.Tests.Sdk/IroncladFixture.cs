// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    /*  NOTE (Cameron):
        Ok, so here is the *amazing* logic around _how_ we want ironclad to run...

        EXTERNAL:       When a developer is executing tests and wants to debug Ironclad then typically they would already have an instance running.
                        -or-
                        When a build server is executing the tests.
                        In this case we do not want to spin up either ironclad or postgres - but we need to check that Ironclad is running (postgres then must be).
        TESTING:        When a developer is executing tests to confirm their code hasn't broken any tests but doesn't care about debugging (also applies to dotnet test).
                        In this case we want to spin up ironclad (from source) and postgres (from a docker container).
        INTEGRATING:    When using the Ironclad.Tests.Sdk to test code written against Ironclad in a separate project.
                        In this case we want to spin up both ironclad and postgres (from docker containers).

        In the above cases, we can default to EXTERNAL mode eg. with no config options specified.
        We can force TESTING through using the 'use_source_code' boolean config value.
        We can force INTEGRATING through using the 'use_docker_image' boolean config value.
        These config values are mutually exclusive, otherwise an configuration exception will be thrown.

        See https://gist.github.com/cameronfletcher/58673a468c8ebbbf91b81e706063ba56 (test.settings) for more information on configuration.  */

    internal class IroncladFixture : IAsyncLifetime
    {
        private readonly Settings settings;
        private readonly PostgresContainer postgres;
        private readonly IAsyncLifetime ironclad;

        public IroncladFixture(IMessageSink messageSink)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true)
                .AddJsonFile("testsettings.Custom.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            this.settings = configuration.GetSection("auth_server").Get<Settings>(options => options.BindNonPublicProperties = true) ?? new Settings();

            if (this.settings.UseSourceCode)
            {
                messageSink?.OnMessage(new DiagnosticMessage(
                    "Authentication fixture is running in TESTING mode (attempting to spin up ironclad from source, postgres from a docker container)"));

                this.postgres = new PostgresContainer(this.settings.PostgresTag);
                this.ironclad = new IroncladBinaries(this.settings.Authority, this.settings.Port, this.postgres.GetConnectionStringForHost());
            }
            else if (this.settings.UseDockerImage)
            {
                messageSink?.OnMessage(new DiagnosticMessage(
                    "Authentication fixture is running in INTEGRATING mode (attempting to spin up both ironclad and postgres from docker containers)"));

                this.postgres = new PostgresContainer(this.settings.PostgresTag, this.settings.OutputDockerLogs);
                this.ironclad = new IroncladContainer(
                    this.settings.Authority,
                    this.settings.Port,
                    this.postgres.GetConnectionStringForContainer(),
                    this.settings.DockerRegistry,
                    this.settings.DockerCredentials,
                    this.settings.DockerTag, this.settings.OutputDockerLogs);
            }
            else
            {
                messageSink?.OnMessage(new DiagnosticMessage("Authentication fixture is running in EXTERNAL mode (attempting to connect to externally running ironclad)"));

                this.postgres = null;
                this.ironclad = new IroncladProbe(this.settings.Authority, 0, 15);
            }
        }

        public string Authority => this.settings.Authority;

        public async Task InitializeAsync()
        {
            if (this.postgres != null)
            {
                await this.postgres.InitializeAsync().ConfigureAwait(false);
            }

            await this.ironclad.InitializeAsync().ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            await this.ironclad.DisposeAsync().ConfigureAwait(false);

            if (this.postgres != null)
            {
                await this.postgres.DisposeAsync().ConfigureAwait(false);
            }
        }

#pragma warning disable IDE1006, SA1000, SA1300, CA1812
        private class Settings
        {
            private static readonly int RandomPort = PortManager.GetNextPort();

            public int Port => this.port == default ? RandomPort : this.port;

            public bool UseDockerImage => this.use_source_code == true ? false : this.use_docker_image ?? true;

            public string DockerRegistry => this.docker?.registry;

            public NetworkCredential DockerCredentials => string.IsNullOrEmpty(this.docker?.username) || string.IsNullOrEmpty(this.docker?.password)
                ? null
                : new NetworkCredential(this.docker.username, this.docker.password);

            public string DockerTag => this.docker?.tag ?? "latest";

            public bool UseSourceCode => this.use_source_code == true;

            public string PostgresTag => this.postgres_tag ?? "alpine";

            public string Authority => $"http://localhost:{this.Port}";

            private int port { get; set; }

            private bool? use_docker_image { get; set; }

            private Docker docker { get; set; }

            private bool? use_source_code { get; set; }

            private string postgres_tag { get; set; }
            
            public bool OutputDockerLogs { get; set; }

            private class Docker
            {
                public string registry { get; set; }

                public string username { get; set; }

                public string password { get; set; }

                public string tag { get; set; }
            }
        }
    }
}