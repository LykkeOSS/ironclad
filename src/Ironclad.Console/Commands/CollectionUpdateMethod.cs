// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using System.Collections.Generic;
    using System.Linq;

    internal class CollectionUpdateMethod<T>
    {
        private bool addOption;
        private bool removeOption;
        private ICollection<T> values;

        private CollectionUpdateMethod()
        {
        }

        public static CollectionUpdateMethod<T> FromOptions(bool addOption, bool removeOption, ICollection<T> values)
        {
            return new CollectionUpdateMethod<T>
                { addOption = addOption, removeOption = removeOption, values = values };
        }

        public ICollection<T> ApplyTo(ICollection<T> src)
        {
            if (!this.removeOption && !this.addOption)
            {
                // assign
                return this.values;
            }

            if (this.addOption && !this.removeOption)
            {
                // add
                return src.Union(this.values).ToList();
            }

            if (this.removeOption && !this.addOption)
            {
                // remove
                return src.Except(this.values).ToList();
            }

            return src;
        }
    }
}