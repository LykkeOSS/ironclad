// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Sdk
{
    using System.Globalization;
    using Ironclad.Extensions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public class SnakeCaseQueryValueProvider : QueryStringValueProvider, IValueProvider
    {
        public SnakeCaseQueryValueProvider(BindingSource bindingSource, IQueryCollection values, CultureInfo culture)
            : base(bindingSource, values, culture)
        {
        }

        public override bool ContainsPrefix(string prefix) => base.ContainsPrefix(prefix.ToSnakeCase());

        public override ValueProviderResult GetValue(string key) => base.GetValue(key.ToSnakeCase());
    }
}
