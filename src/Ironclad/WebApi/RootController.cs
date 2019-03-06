// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.WebApi
{
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    public class RootController : Controller
    {
        private readonly ApiInfo apiInfo;

        public RootController(ApiInfo apiInfo)
        {
            this.apiInfo = apiInfo;
        }

        [HttpGet]
        public IActionResult Get() => this.Ok(this.apiInfo);
    }
}
