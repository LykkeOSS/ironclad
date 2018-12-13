// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result as ViewResult;
            if (result == null)
            {
                return;
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
            }

            // https://scotthelme.co.uk/hsts-the-missing-link-in-tls/
            var hsts = "Strict-Transport-Security: max-age=31536000; includeSubDomains";
            if (!context.HttpContext.Response.Headers.ContainsKey("Strict-Transport-Security"))
            {
                context.HttpContext.Response.Headers.Add("Strict-Transport-Security", hsts);
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
            string[] allowedScripts = new[]
            {
                "'self'",
                "'sha256-SjXRkVC/0M0+WLq2GU4E8JdbZ/ZNgspoHSzWQaMhG7E='",
                "'sha256-nEhC/Gar4FM2L9jcHtSP+DYaxFlJNy7jG8KD46S2SlI='",
                "'sha256-od6N/tEDLReTkDWYLIQ4wfOY9HAR4vm2mqgP2a8XdTU='",
                "'sha256-aqNNdDLnnrDOnTNdkJpYlAxKVJtLt9CtFLklmInuUAE='",
                "'sha256-RBBmlHImRT323C+VE9PFBMXnHbki8sYW3t1e6JhINnU='",
                /* App Insights */
                "https://az416426.vo.msecnd.net/scripts/a/ai.0.js",
                "https://ajax.aspnetcdn.com/ajax/jquery.validate/1.14.0/jquery.validate.min.js",
                "https://ajax.aspnetcdn.com/ajax/jquery.validation.unobtrusive/3.2.6/jquery.validate.unobtrusive.min.js"
            };

            string[] allowedStyles = new[]
            {
                "'self'",
                /* Remove this once this GitHub issue will be solved
                 * https://github.com/aspnet/Mvc/issues/4888 */
               "'unsafe-inline'"
            };

            var csp = $"script-src {string.Join(" ", allowedScripts)}; img-src 'self' data:; style-src {string.Join(" ", allowedStyles)}; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';";

            // once for standards compliant browsers
            if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.HttpContext.Response.Headers.Add("Content-Security-Policy", csp);
            }

            // and once again for IE
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
            {
                context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
            var referrer_policy = "no-referrer";
            if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
            }

            // https://scotthelme.co.uk/hardening-your-http-response-headers/#x-xss-protection
            if (!context.HttpContext.Response.Headers.ContainsKey("X-XSS-Protection"))
            {
                context.HttpContext.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            }

            // https://scotthelme.co.uk/a-new-security-header-feature-policy/
            var features = "geolocation 'none'; microphone 'none'; camera 'none'";
            if (!context.HttpContext.Response.Headers.ContainsKey("Feature-Policy"))
            {
                context.HttpContext.Response.Headers.Add("Feature-Policy", features);
            }
        }
    }
}