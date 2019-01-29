// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Configuration
{
    using System.Collections.Generic;
    using IdentityServer4;
    using IdentityServer4.Models;

    public static partial class Config
    {
        public static IEnumerable<Client> GetDefaultClients() =>
            new List<Client>
            {
                // NOTE (Cameron): This is the sample client representing server-to-server communication.
                new Client
                {
                    ClientId = "sample_client",
                    ClientName = "Sample Client Application",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedScopes = { "sample_api" },
                },

                // NOTE (Cameron): This is the sample single page application.
                new Client
                {
                    ClientId = "sample_spa",
                    ClientName = "Sample Single Page Application",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { "http://localhost:5008/callback.html" },
                    PostLogoutRedirectUris = { "http://localhost:5008/index.html" },
                    AllowedCorsOrigins = { "http://localhost:5008" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "sample_api",
                    },

                    AccessTokenType = AccessTokenType.Reference,
                },

                // NOTE (Cameron): This is the sample console client representing hybrid communication.
                new Client
                {
                    ClientId = "sample_console",
                    ClientName = "Sample Console Client (Hybrid with PKCE)",

                    RequireClientSecret = false,

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    RequirePkce = true,

                    RedirectUris = { "http://127.0.0.1" },

                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "sample_api",
                    },
                },

                // NOTE (Cameron): This is the sample client representing hybrid/client credentials communication.
                new Client
                {
                    ClientId = "sample_mvc",
                    ClientName = "Sample MVC Application",

                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    LogoUri = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSNVmKVcOIDLIeHBTd49h9ssgWP9GqEPH66YK8HxYZ75-EaN7Kx",

                    RedirectUris = { "http://localhost:5009/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:5009/signout-callback-oidc" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "sample_api",
                    },

                    AllowOfflineAccess = true
                },

                new Client
                {
                    ClientId = "auth_console",
                    ClientName = "Authorization Server Management Console",

                    RequireClientSecret = false,

                    AllowedGrantTypes = GrantTypes.Hybrid,
                    RequirePkce = true,

                    RedirectUris = { "http://127.0.0.1" },

                    AllowOfflineAccess = true,

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "auth_api",
                    },

                    RequireConsent = false,
                    AccessTokenType = AccessTokenType.Reference,
                },
            };
    }
}
