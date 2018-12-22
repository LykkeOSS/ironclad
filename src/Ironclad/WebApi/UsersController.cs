// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

using Marten;

#pragma warning disable CA1308

namespace Ironclad.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Application;
    using Client;
    using Configuration;
    using IdentityModel;
    using IdentityServer4.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Services.Email;

    [Authorize("user_admin")]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private const string AspNetIdentitySecurityStamp = "AspNet.Identity.SecurityStamp";

        private static readonly IEqualityComparer<Claim> ClaimComparer = new ClaimEqualityComparer();

        private static readonly HashSet<string> ReservedClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            JwtClaimTypes.Subject,
            JwtClaimTypes.Role,
            JwtClaimTypes.PreferredUserName,
            JwtClaimTypes.Name,
            JwtClaimTypes.Email,
            JwtClaimTypes.PhoneNumber,
            AspNetIdentitySecurityStamp,
        };

        private static readonly HashSet<string> SpecialClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            JwtClaimTypes.EmailVerified,
            JwtClaimTypes.PhoneNumberVerified
        };

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory;
        private readonly IEmailSender emailSender;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.claimsFactory = claimsFactory;
            this.emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string username, int skip = default, int take = 20)
        {
            skip = Math.Max(0, skip);
            take = take < 0 ? 20 : Math.Min(take, 100);

            var userQuery = string.IsNullOrEmpty(username)
                ? this.userManager.Users
                : this.userManager.Users.Where(user => user.UserName.StartsWith(username, StringComparison.OrdinalIgnoreCase));

            var totalSize = await userQuery.CountAsync();
            var users = await userQuery.OrderBy(user => user.UserName).Skip(skip).Take(take).ToListAsync();
            var resources = users.Select(
                user =>
                new UserSummaryResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + user.UserName),
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                });

            var resourceSet = new ResourceSet<UserSummaryResource>(skip, totalSize, resources);

            return this.Ok(resourceSet);
        }

        [HttpHead("{username}")]
        [HttpGet("{username}")]
        public async Task<IActionResult> Get(string username)
        {
            // HACK (Pawel): This is a temporary measure until we have a sensible way to resolve subject identifiers with username etc.
            // TODO (Cameron): "temporary"
            var user = await this.userManager.FindByNameAsync(username) ?? await this.userManager.FindByIdAsync(username);
            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var roles = await this.userManager.GetRolesAsync(user);
            var logins = await this.userManager.GetLoginsAsync(user);
            var claimsPrincipal = await this.claimsFactory.CreateAsync(user);
            var claims = new JwtSecurityToken(new JwtHeader(), new JwtPayload(claimsPrincipal.Claims)).Payload;
            var externalLoginProviders = logins.Select(login => login.ProviderDisplayName).ToList();

            claims.Remove(AspNetIdentitySecurityStamp);
            claims.Remove(JwtClaimTypes.Role);

            return this.Ok(
                new UserResource
                {
                    Url = this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + user.UserName),
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = new List<string>(roles),
                    Claims = claims,
                    ExternalLoginProviders = externalLoginProviders.Count > 0 ? externalLoginProviders : null,
                });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]User model)
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                return this.BadRequest(new { Message = $"Cannot create a user without a username" });
            }

            if (model.Roles != null)
            {
                foreach (var role in model.Roles)
                {
                    if (!await this.roleManager.RoleExistsAsync(role))
                    {
                        return this.BadRequest(new { Message = $"Cannot create a user with the role '{role}' when that role does not exist" });
                    }
                }
            }

            if (model.Claims?.Any(claim => string.IsNullOrEmpty(claim.Key) || claim.Value == null) == true)
            {
                return this.BadRequest(new { Message = $"Cannot add claims without both a type and a value" });
            }

            if (model.Claims?.Any(claim => ReservedClaims.Contains(claim.Key)) == true)
            {
                return this.BadRequest(new { Message = $"Cannot change reserved claims values" });
            }

            var isConfirmEmailRequest = default(bool);
            if (model.Claims != null &&
                model.Claims.TryGetValue(JwtClaimTypes.EmailVerified, out var emailVerifiedObject) &&
                (!bool.TryParse(emailVerifiedObject.ToString(), out isConfirmEmailRequest) || isConfirmEmailRequest != true))
            {
                return this.BadRequest(new { Message = $"Invalid value for claim '{JwtClaimTypes.EmailVerified}' (only valid value is 'true')" });
            }

            var isConfirmPhoneNumberRequest = default(bool);
            if (model.Claims != null &&
                model.Claims.TryGetValue(JwtClaimTypes.PhoneNumberVerified, out var phoneNumberVerifiedObject) &&
                (!bool.TryParse(phoneNumberVerifiedObject.ToString(), out isConfirmPhoneNumberRequest) || isConfirmPhoneNumberRequest != true))
            {
                return this.BadRequest(new { Message = $"Invalid value for claim '{JwtClaimTypes.PhoneNumberVerified}' (only valid value is 'true')" });
            }

            var user = new ApplicationUser(model.Username);

            // optional properties
            user.Email = model.Email ?? user.Email;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            var addUserResult = string.IsNullOrEmpty(model.Password) ? await this.userManager.CreateAsync(user) : await this.userManager.CreateAsync(user, model.Password);
            if (!addUserResult.Succeeded)
            {
                if (addUserResult.Errors.Any(error => error.Code == "DuplicateUserName"))
                {
                    return this.StatusCode((int)HttpStatusCode.Conflict, new { Message = "User already exists" });
                }

                return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addUserResult.ToString() });
            }

            if (!string.IsNullOrEmpty(model.ExternalLoginProvider))
            {
                var claims = new[] { new Claim(JwtClaimTypes.Name, model.Username) };
                var identity = new ClaimsIdentity(claims, model.ExternalLoginProvider, "name", "role");
                var principal = new ClaimsPrincipal(identity);
                var info = new ExternalLoginInfo(principal, model.ExternalLoginProvider, model.Username, model.Username);

                var addLoginResult = await this.userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addLoginResult.ToString() });
                }
            }

            if (model.Roles != null)
            {
                var addToRolesResult = await this.userManager.AddToRolesAsync(user, model.Roles);
                if (!addToRolesResult.Succeeded)
                {
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addToRolesResult.ToString() });
                }
            }

            if (model.Claims?.Count > 0)
            {
                var claims = model.Claims.Where(claim => !SpecialClaims.Contains(claim.Key)).Select(claim => new Claim(claim.Key.ToLowerInvariant(), claim.Value.ToString()));

                var addToClaimsResult = await this.userManager.AddClaimsAsync(user, claims);
                if (!addToClaimsResult.Succeeded)
                {
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addToClaimsResult.ToString() });
                }

                if (isConfirmEmailRequest && !user.EmailConfirmed)
                {
                    var token = await this.userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmResult = await this.userManager.ConfirmEmailAsync(user, token);
                    if (!confirmResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = confirmResult.ToString() });
                    }
                }

                if (isConfirmPhoneNumberRequest && !user.PhoneNumberConfirmed)
                {
                    var token = await this.userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
                    var confirmResult = await this.userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, token);
                    if (!confirmResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = confirmResult.ToString() });
                    }
                }
            }

            var callbackUrl = default(string);

            if (string.IsNullOrEmpty(model.Password) && !string.IsNullOrEmpty(model.Email))
            {
                var code = await this.userManager.GeneratePasswordResetTokenAsync(user);
                callbackUrl = this.Url.CompleteRegistrationLink(user.Id, code, this.Request.Scheme);

                if (model.SendConfirmationEmail != false)
                {
                    await this.emailSender.SendActivationEmailAsync(model.Email, callbackUrl);
                }
            }

            return this.Created(
                new Uri(this.HttpContext.GetIdentityServerRelativeUrl("~/api/users/" + model.Username)),
                callbackUrl != null ? new { registrationLink = callbackUrl } : null);
        }

        [HttpPut("{username}")]
        public async Task<IActionResult> Put(string username, [FromBody]User model)
        {
            var user = await this.userManager.FindByNameAsync(username) ?? await this.userManager.FindByIdAsync(username);
            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            if (user.Id == Config.DefaultAdminUserId && model.Roles != null && !model.Roles.Contains("admin"))
            {
                return this.BadRequest(new { Message = $"Cannot remove the role 'admin' from the default admin user" });
            }

            if (model.Roles != null)
            {
                foreach (var role in model.Roles)
                {
                    if (!await this.roleManager.RoleExistsAsync(role))
                    {
                        return this.BadRequest(new { Message = $"Cannot modify a user with the role '{role}' when that role does not exist" });
                    }
                }
            }

            if (model.Claims?.Any(claim => string.IsNullOrEmpty(claim.Key) || claim.Value == null) == true)
            {
                return this.BadRequest(new { Message = $"Cannot add claims without both a type and a value" });
            }

            if (model.Claims?.Any(claim => ReservedClaims.Contains(claim.Key)) == true)
            {
                return this.BadRequest(new { Message = $"Cannot change reserved claims values" });
            }

            var isConfirmEmailRequest = default(bool);
            if (model.Claims != null &&
                model.Claims.TryGetValue(JwtClaimTypes.EmailVerified, out var emailVerifiedObject) &&
                (!bool.TryParse(emailVerifiedObject.ToString(), out isConfirmEmailRequest) || isConfirmEmailRequest != true))
            {
                return this.BadRequest(new { Message = $"Invalid value for claim '{JwtClaimTypes.EmailVerified}' (only valid value is 'true')" });
            }

            var isConfirmPhoneNumberRequest = default(bool);
            if (model.Claims != null &&
                model.Claims.TryGetValue(JwtClaimTypes.PhoneNumberVerified, out var phoneNumberVerifiedObject) &&
                (!bool.TryParse(phoneNumberVerifiedObject.ToString(), out isConfirmPhoneNumberRequest) || isConfirmPhoneNumberRequest != true))
            {
                return this.BadRequest(new { Message = $"Invalid value for claim '{JwtClaimTypes.PhoneNumberVerified}' (only valid value is 'true')" });
            }

            user.UserName = model.Username ?? user.UserName;
            user.Email = model.Email ?? user.Email;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

            var result = await this.userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = result.ToString() });
            }

            if (model.Roles != null)
            {
                var roles = await this.userManager.GetRolesAsync(user);

                var oldRoles = roles.Except(model.Roles ?? Array.Empty<string>()).ToArray();
                if (oldRoles.Length > 0)
                {
                    var removeResult = await this.userManager.RemoveFromRolesAsync(user, oldRoles);
                    if (!removeResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = removeResult.ToString() });
                    }
                }

                var newRoles = (model.Roles ?? Array.Empty<string>()).Except(roles).ToArray();
                if (newRoles.Length > 0)
                {
                    var addResult = await this.userManager.AddToRolesAsync(user, newRoles);
                    if (!addResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addResult.ToString() });
                    }
                }
            }

            if (model.Claims != null)
            {
                var claims = await this.userManager.GetClaimsAsync(user);
                var userClaims = model.Claims
                    .Where(claim => !new[] { "email_verified", "phone_number_verified" }.Contains(claim.Key))
                    .Select(claim => new Claim(claim.Key, claim.Value.ToString())).ToArray();

                var oldClaims = claims.Except(userClaims, ClaimComparer).ToArray();
                if (oldClaims.Length > 0)
                {
                    var removeResult = await this.userManager.RemoveClaimsAsync(user, oldClaims);
                    if (!removeResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = removeResult.ToString() });
                    }
                }

                var newClaims = userClaims.Except(claims, ClaimComparer).ToArray();
                if (newClaims.Length > 0)
                {
                    var addResult = await this.userManager.AddClaimsAsync(user, newClaims);
                    if (!addResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = addResult.ToString() });
                    }
                }

                if (isConfirmEmailRequest && !user.EmailConfirmed)
                {
                    var token = await this.userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmResult = await this.userManager.ConfirmEmailAsync(user, token);
                    if (!confirmResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = confirmResult.ToString() });
                    }
                }

                if (isConfirmPhoneNumberRequest && !user.PhoneNumberConfirmed)
                {
                    var token = await this.userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
                    var confirmResult = await this.userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, token);
                    if (!confirmResult.Succeeded)
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, new { Message = confirmResult.ToString() });
                    }
                }
            }

            return this.NoContent();
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> Delete(string username)
        {
            var user = await this.userManager.FindByNameAsync(username);
            if (user == null)
            {
                return this.Ok();
            }

            if (user.Id == Config.DefaultAdminUserId)
            {
                return this.BadRequest(new { Message = $"Cannot remove the default admin user" });
            }

            await this.userManager.DeleteAsync(user);

            return this.NoContent();
        }

        [HttpGet("{username}/claims")]
        public async Task<IActionResult> GetClaimsAsync(string username)
        {
            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var claimsPrincipal = await this.claimsFactory.CreateAsync(user);
            var claims = new JwtSecurityToken(new JwtHeader(), new JwtPayload(claimsPrincipal.Claims)).Payload;

            claims.Remove(AspNetIdentitySecurityStamp);
            claims.Remove(JwtClaimTypes.Role);

            return this.Ok(claims);
        }

        [HttpPost("{username}/claims")]
        public async Task<IActionResult> AddClaimsAsync(
            string username,
            [FromBody] IDictionary<string, List<object>> claims)
        {
            if (claims.Keys.Any(string.IsNullOrWhiteSpace))
            {
                return this.BadRequest(new { Message = "Cannot add empty claim" });
            }

            if (claims.Values.Any(x => x == null || x.IsEmpty()))
            {
                return this.BadRequest(new { Message = "Cannot add empty claim value" });
            }

            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var claimsPrincipal = await this.claimsFactory.CreateAsync(user);

            var filteredClaims = claims.Keys.Except(ReservedClaims)
                .SelectMany(key => claims[key].Select(value => new Claim(key.ToLowerInvariant(), value.ToString())));

            foreach (var claim in filteredClaims)
            {
                if (claimsPrincipal.HasClaim(claim.Type, claim.Value))
                {
                    continue;
                }

                var identityResult = await this.userManager.AddClaimAsync(user, claim);

                if (!identityResult.Succeeded)
                {
                    return this.StatusCode(
                        (int)HttpStatusCode.InternalServerError,
                        new { Message = identityResult.ToString() });
                }
            }

            return this.NoContent();
        }

        [HttpDelete("{username}/claims")]
        public async Task<IActionResult> RemoveClaimsAsync(
            string username,
            [FromBody] IDictionary<string, List<object>> claims)
        {
            if (claims.Keys.Any(string.IsNullOrWhiteSpace))
            {
                return this.BadRequest(new { Message = "Cannot remove empty claim" });
            }

            if (claims.Values.Any(x => x == null || x.IsEmpty()))
            {
                return this.BadRequest(new { Message = "Cannot remove empty claim value" });
            }

            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var filteredClaims = claims.Keys.Except(ReservedClaims)
                .SelectMany(key => claims[key].Select(value => new Claim(key.ToLowerInvariant(), value.ToString())));

            var identityResult = await this.userManager.RemoveClaimsAsync(user, filteredClaims);

            if (!identityResult.Succeeded)
            {
                return this.StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new { Message = identityResult.ToString() });
            }

            return this.NoContent();
        }

        [HttpGet("{username}/roles")]
        public async Task<IActionResult> GetRolesAsync(string username)
        {
            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var roles = await this.userManager.GetRolesAsync(user);

            return this.Ok(roles);
        }

        [HttpPost("{username}/roles")]
        public async Task<IActionResult> AddToRolesAsync(string username, [FromBody] List<string> roles)
        {
            if (roles.IsEmpty() || roles.Any(string.IsNullOrWhiteSpace))
            {
                return this.BadRequest(new { Message = "Cannot add user to empty role" });
            }

            foreach (var role in roles)
            {
                if (!await this.roleManager.RoleExistsAsync(role))
                {
                    return this.BadRequest(new { Message = $"The role {role} does not exist" });
                }
            }

            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            var identityResult = await this.userManager.AddToRolesAsync(user, roles);

            if (!identityResult.Succeeded)
            {
                return this.StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new { Message = identityResult.ToString() });
            }

            return this.NoContent();
        }

        [HttpDelete("{username}/roles")]
        public async Task<IActionResult> RemoveFromRolesAsync(string username, [FromBody] List<string> roles)
        {
            if (roles.IsEmpty() || roles.Any(string.IsNullOrWhiteSpace))
            {
                return this.BadRequest(new { Message = "Cannot remove user from empty role" });
            }

            var user = await this.userManager.FindByNameAsync(username) ??
                       await this.userManager.FindByIdAsync(username);

            if (user == null)
            {
                return this.NotFound(new { Message = $"User '{username}' not found" });
            }

            if (user.Id == Config.DefaultAdminUserId && roles.Contains("admin"))
            {
                return this.BadRequest(new { Message = $"Cannot remove the role 'admin' from the default admin user" });
            }

            var identityResult = await this.userManager.RemoveFromRolesAsync(user, roles);

            if (!identityResult.Succeeded)
            {
                return this.StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    new { Message = identityResult.ToString() });
            }

            return this.NoContent();
        }

#pragma warning disable CA1034, CA1056
        private class UserResource : User
        {
            public string Url { get; set; }
        }

        private class UserSummaryResource : UserSummary
        {
            public string Url { get; set; }
        }

        private class ClaimEqualityComparer : IEqualityComparer<Claim>
        {
            public bool Equals(Claim left, Claim right)
            {
                if (left == null && right == null)
                {
                    return true;
                }

                if (left == null || right == null)
                {
                    return false;
                }

                return string.Equals(left.Type, right.Type, StringComparison.OrdinalIgnoreCase) && string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Claim obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + obj.Type?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
                    hash = (hash * 23) + obj.Value?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
                    return hash;
                }
            }
        }
    }
}
