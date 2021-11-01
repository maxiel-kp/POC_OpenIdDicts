using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ClientCredentials.Server.Controllers
{
    public class AuthorizationController : ControllerBase
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager
            , IOpenIddictScopeManager scopeManager
            , IOpenIddictAuthorizationManager authorizationManager)
        {
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _authorizationManager = authorizationManager;
        }

        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (!request.IsClientCredentialsGrantType() && !request.IsRefreshTokenGrantType())
            {
                throw new NotImplementedException("The specified grant type is not implemented.");
            }

            var principal = new ClaimsPrincipal();
            if (request.IsRefreshTokenGrantType())
            {
                principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
            }
            else if (request.IsClientCredentialsGrantType())
            {
                var scopes = request.GetScopes();
                var destinations = new[] { Destinations.AccessToken, Destinations.IdentityToken };

                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.
                var application = await GetApplication(request.ClientId)
                                    ?? throw new InvalidOperationException("The application details cannot be found in the database.");

                // Create a new ClaimsIdentity containing the claims that
                // will be used to create an id_token, a token or a code.
                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

                // Use the client_id as the subject identifier.
                identity.AddClaim(Claims.Subject, application.ClientId, destinations);
                identity.AddClaim(Claims.Name, application.DisplayName, destinations);

                // Create new principal.
                principal = new ClaimsPrincipal(identity);
                principal.SetScopes(scopes);// Add all scope
                principal.SetResources(await _scopeManager.ListResourcesAsync(scopes).ToListAsync()); // Add resource scope

                // Scopes.OfflineAccess สำหรับทำ refresh token
                var authorizationScopes = scopes.Where(w => (new[] { Scopes.OpenId, Scopes.OfflineAccess }).Contains(w))
                                                .ToImmutableArray();
                // Get authorization
                var authorization = await GetLastAuthorizations(request.ClientId, application.Id, authorizationScopes);

                // Automatically create a permanent authorization to avoid requiring explicit consent
                // for future authorization or token requests containing the same scopes.
                if (authorization == null)
                {
                    authorization = await CreateAuthorization(principal, request.ClientId, application.Id, authorizationScopes);
                }
                principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
            }

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

        private async Task<OpenIddictEntityFrameworkCoreAuthorization> GetLastAuthorizations
            (string subject, string client, ImmutableArray<string> scopes)
        {
            var authorizes = await _authorizationManager.FindAsync(
                subject, client,
                Statuses.Valid,
                AuthorizationTypes.Permanent,
                scopes).ToListAsync();

            return authorizes.Select(s => s as OpenIddictEntityFrameworkCoreAuthorization)
                             .OrderByDescending(o => o.CreationDate)
                             .FirstOrDefault();
        }

        private async Task<OpenIddictEntityFrameworkCoreAuthorization> CreateAuthorization
            (ClaimsPrincipal principal, string subject, string client, ImmutableArray<string> scopes)
        {
            var authorization = await _authorizationManager.CreateAsync(
                principal: principal,
                subject: subject,
                client: client,
                type: AuthorizationTypes.Permanent,
                scopes: scopes);

            return authorization as OpenIddictEntityFrameworkCoreAuthorization;
        }

        private async Task<OpenIddictEntityFrameworkCoreApplication> GetApplication(string clientId)
        {
            var result = await _applicationManager.FindByClientIdAsync(clientId);
            return result as OpenIddictEntityFrameworkCoreApplication;
        }

    }
}