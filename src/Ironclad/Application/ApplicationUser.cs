// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Application
{
    using System;
    using System.Globalization;
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            this.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        public ApplicationUser(string username)
            : this()
        {
            this.UserName = username;
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
