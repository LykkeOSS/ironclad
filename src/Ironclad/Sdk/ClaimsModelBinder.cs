// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class ClaimsModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            using (var reader = new StreamReader(bindingContext.HttpContext.Request.Body))
            {
                var content = reader.ReadToEnd();
                var payload = JwtPayload.Deserialize(content);

                bindingContext.Result = ModelBindingResult.Success(payload.Claims);
            }

            return Task.CompletedTask;
        }
    }
}
