// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Extensions
{
    using System;
    using Serilog.Core;
    using Serilog.Events;

    public class AzureInstanceIdSerilogEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");

            if (instanceId != null)
            {
                if (instanceId.Length > 6)
                {
                    instanceId = instanceId.Substring(0, 6);
                }

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                        "InstanceId", instanceId));
            }
        }
    }
}
