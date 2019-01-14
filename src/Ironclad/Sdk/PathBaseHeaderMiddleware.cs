// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class PathBaseHeaderMiddleware
    {
        private readonly RequestDelegate next;

        public PathBaseHeaderMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
            {
                context.Request.PathBase = pathBases.First();
            }

            return this.next.Invoke(context);
        }
    }
}