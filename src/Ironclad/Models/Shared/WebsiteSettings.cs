// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Models
{
    using System.Collections.Generic;

    public class WebsiteSettings
    {
        public string Styles { get; set; } = "css/default.css";

        public string Logo { get; set; } = "img/logo_white.svg";

        public string DarkLogo { get; set; } = "img/logo.svg";

        // HACK (Cameron): This really doesn't belong here.
        public IEnumerable<string> RestrictedDomains { get; set; }

        public bool ShowLoginScreen => this.show_login_screen.HasValue ? this.show_login_screen.Value : true;

#pragma warning disable IDE1006, SA1300
        private bool? show_login_screen { get; set; }
    }
}
