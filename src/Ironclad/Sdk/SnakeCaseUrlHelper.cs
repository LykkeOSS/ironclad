// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using Ironclad.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;

    internal class SnakeCaseUrlHelper : UrlHelper
    {
        public SnakeCaseUrlHelper(ActionContext actionContext)
            : base(actionContext)
        {
        }

        protected override VirtualPathData GetVirtualPathData(string routeName, RouteValueDictionary values)
        {
            var snakeCaseValues = new RouteValueDictionary();

            foreach (var value in values)
            {
                snakeCaseValues.Add(value.Key.ToSnakeCase(), value.Value);
            }

            return base.GetVirtualPathData(routeName, snakeCaseValues);
        }
    }
}
