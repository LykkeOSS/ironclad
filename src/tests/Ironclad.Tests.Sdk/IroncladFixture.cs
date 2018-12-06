﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Tests.Sdk
{
    using System.Configuration;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Npgsql;
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
        These config values are mutually exclusive, otherwise an configuration exception will be thrown.  */

    internal class IroncladFixture : IAsyncLifetime
    {
        private readonly IAsyncLifetime postgres;
        private readonly IAsyncLifetime ironclad;

        public IroncladFixture(IMessageSink messageSink)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("testsettings.json").Build();

            var settings = configuration.GetSection("auth_server").Get<Settings>(options => options.BindNonPublicProperties = true);
            settings.Validate();

            // TODO (Cameron): This needs to be moved as we should be able to host dynamically if not running in EXTERNAL mode.
            var authority = configuration.GetValue<string>("authority") ?? throw new ConfigurationErrorsException("Missing configuration value 'authority'");
            var connectionString = "Host=localhost;Database=ironclad;Username=postgres;Password=postgres;";

            if (settings.UseSourceCode)
            {
                messageSink?.OnMessage(new DiagnosticMessage(
                    "Authentication fixture is running in TESTING mode (attempting to spin up ironclad from source, postgres from a docker container)"));

                this.postgres = new PostgresContainer(new NpgsqlConnectionStringBuilder(connectionString));
                this.ironclad = new IroncladBinaries(authority, connectionString);
            }
            else if (settings.UseDockerImage)
            {
                messageSink?.OnMessage(new DiagnosticMessage(
                    "Authentication fixture is running in INTEGRATING mode (attempting to spin up both ironclad and postgres from docker containers)"));

                this.postgres = new PostgresContainer(new NpgsqlConnectionStringBuilder(connectionString));
                this.ironclad = new IroncladContainer(authority, "Host=host.docker.internal;Database=ironclad;Username=postgres;Password=postgres;");
            }
            else
            {
                messageSink?.OnMessage(new DiagnosticMessage("Authentication fixture is running in EXTERNAL mode (attempting to connect to externally running ironclad)"));

                this.postgres = null;
                this.ironclad = new IroncladProbe(authority, 0, 15);
            }
        }

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

#pragma warning disable IDE1006, SA1300, CA1812
        private class Settings
        {
            public bool UseSourceCode => this.use_source_code;

            public bool UseDockerImage => this.use_docker_image;

            private bool use_source_code { get; set; }

            private bool use_docker_image { get; set; }

            public void Validate()
            {
                if (this.use_docker_image && this.use_source_code)
                {
                    throw new ConfigurationErrorsException(
                        $"Cannot set values of both auth_server:{nameof(this.use_source_code)} and auth_server:{nameof(this.use_docker_image)} to true.");
                }
            }
        }
    }
}