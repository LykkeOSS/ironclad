// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.
#pragma warning disable CA1034 // Nested types should not be visible
namespace Ironclad.WebApi
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using Microsoft.Extensions.Configuration;

    public class ApiInfo
    {
        public ApiInfo(IConfiguration configuration)
        {
            this.Title = typeof(Program).Assembly.Attribute<AssemblyTitleAttribute>(attribute => attribute.Title);
            this.Version = typeof(Program).Assembly.Attribute<AssemblyInformationalVersionAttribute>(attribute => attribute.InformationalVersion);
            this.OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription.TrimEnd();
            this.ProcessId = Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);

            if (Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") != null)
            {
                this.Azure = new ApiInfo.AzureInfo
                {
                    InstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
                    SiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"),
                };
            }

            if (configuration.GetValue<string>("git:branch") != null)
            {
                this.Git = new GitInfo
                {
                    AuthorDate = configuration.GetValue<string>("git:authorDate"),
                    Branch = configuration.GetValue<string>("git:branch"),
                    CommitSha = configuration.GetValue<string>("git:commitSha"),
                };
            }
        }

        public string Title { get; }

        public string Version { get; }

        public string OS { get; }

        public string ProcessId { get; }

        public AzureInfo Azure { get; }

        public GitInfo Git { get; }

        public class AzureInfo
        {
            public string InstanceId { get; set; }

            public string SiteName { get; set; }
        }

        public class GitInfo
        {
            public string AuthorDate { get; set; }

            public string Branch { get; set; }

            public string CommitSha { get; set; }
        }
    }
}
