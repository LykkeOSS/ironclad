// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public class AuthCookieMiddleware
    {
        private readonly RequestDelegate next;

        public AuthCookieMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var handlerProvider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();

            foreach (var scheme in await schemeProvider.GetRequestHandlerSchemesAsync())
            {
                if (await handlerProvider.GetHandlerAsync(context, scheme.Name) is IAuthenticationRequestHandler handler &&
                    await handler.HandleRequestAsync())
                {
                    string location = null;
                    if (context.Response.StatusCode == (int)HttpStatusCode.Redirect)
                    {
                        location = context.Response.Headers["location"];
                    }
                    else if (context.Request.Method == "GET" && !context.Request.Query["skip"].Any())
                    {
                        location = context.Request.Path + context.Request.QueryString + "&skip=1";
                    }

                    if (location != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        var html = $@"
                                <html><head>
                                    <meta http-equiv='refresh' content='0;url={location}' />
                                </head></html>";
                        await context.Response.WriteAsync(html);
                    }

                    return;
                }
            }

            await this.next.Invoke(context);
        }
    }
}