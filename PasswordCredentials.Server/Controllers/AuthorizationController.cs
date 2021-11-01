using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IC.Common.Web.DTOs;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.SystemFunctions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PasswordCredentials.Server.Data;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PasswordCredentials.Server.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController, ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/connect")]
    public class AuthorizationController : Controller
    {
        private readonly SignInManager<UserAccount> _signInManager;
        private readonly UserManager<UserAccount> _userManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        /// <summary>
        /// 
        /// </summary>
        public AuthorizationController(SignInManager<UserAccount> signInManager
            , UserManager<UserAccount> userManager
            , IOpenIddictScopeManager scopeManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _scopeManager = scopeManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var principal = new ClaimsPrincipal();
            if (request.IsPasswordGrantType())
            {
                var user = await _userManager.FindByNameAsync(request.Username);
                if (user == null)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Validate the username/password parameters and ensure the account is not locked out.
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                var scopes = request.GetScopes();

                // Create a new ClaimsPrincipal containing the claims that
                // will be used to create an id_token, a token or a code.
                principal = await _signInManager.CreateUserPrincipalAsync(user);

                principal.SetClaims(Claims.Role, ImmutableArray.Create("admin", "account"));

                // Set the list of scopes granted to the client application.
                principal.SetScopes((new[]
                {
                    Scopes.Email,
                    Scopes.Profile,
                    Scopes.Roles,
                    Scopes.OpenId,
                    Scopes.OfflineAccess
                }).Intersect(scopes));

                principal.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync());

                foreach (var claim in principal.Claims)
                {
                    //claim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
                    claim.SetDestinations(GetDestinations(claim, principal));
                }
            }
            else if (request.IsRefreshTokenGrantType())
            {
                principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            else
            {
                throw new NotImplementedException("The specified grant type is not implemented.");
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            //var signInResult = SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            //return Ok(new BaseResponseResult
            //{
            //    IsSuccess = true,
            //    Result = signInResult
            //});
        }

        private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            switch (claim.Type)
            {
                case Claims.Name:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Email))
                        yield return Destinations.IdentityToken;

                    yield break;

                case Claims.Role:
                    yield return Destinations.AccessToken;

                    if (principal.HasScope(Scopes.Roles))
                        yield return Destinations.IdentityToken;

                    yield break;

                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                case "AspNet.Identity.SecurityStamp": yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }


    }
}