// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1724

namespace Ironclad.Tests.Sdk
{
    using System;
    using System.Threading.Tasks;
    using IdentityModel.OidcClient.Browser;

    internal class Browser : IBrowser
    {
        private readonly BrowserAutomation automation;
        private readonly string path;

        public Browser(BrowserAutomation automation, int? port = null, string path = null)
        {
            this.automation = automation;
            this.path = path;
            this.Port = port ?? PortManager.GetNextPort();
        }

        public int Port { get; }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options)
        {
            using (var listener = new LoopbackHttpListener(this.Port, this.path))
            {
                await this.automation.NavigateToLoginAsync(options.StartUrl).ConfigureAwait(false);
                await this.automation.LoginToAuthorizationServerAsync().ConfigureAwait(false);

                try
                {
                    var result = await listener.WaitForCallbackAsync(5).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Empty response." };
                    }

                    return new BrowserResult { Response = result, ResultType = BrowserResultType.Success };
                }
                catch (TaskCanceledException ex)
                {
                    return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = ex.Message };
                }
                catch (Exception ex)
                {
                    return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
                }
            }
        }
    }
}