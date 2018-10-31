// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.WebApi
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using Ironclad.Models;
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    public class RootController : Controller
    {
        private static readonly object Version =
            new
            {
                Title = typeof(Program).Assembly.Attribute<AssemblyTitleAttribute>(attribute => attribute.Title),
                Version = typeof(Program).Assembly.Attribute<AssemblyInformationalVersionAttribute>(attribute => attribute.InformationalVersion),
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription.TrimEnd(),
                ProcessId = Process.GetCurrentProcess().Id,
                AzureApp = new AzureAppInfo
                {
                    InstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
                    SiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"),
                }
            };

        [HttpGet]
        public IActionResult Get() => this.Ok(Version);
    }
}
