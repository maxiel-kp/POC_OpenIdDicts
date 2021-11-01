using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using PasswordCredentials.Server.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PasswordCredentials.Server
{
    public class Worker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public Worker(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();

                var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

                if (await applicationManager.FindByClientIdAsync("server_api_1") == null)
                {
                    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = "server_api_1",
                        ClientSecret = "07EA7AF4-32E4-4D35-B1AD-A94F7C3E0B43",
                        DisplayName = "Server Api 1",
                        ConsentType = ConsentTypes.Explicit,
                        Type = ClientTypes.Confidential,
                        Permissions =
                        {
                            Permissions.Endpoints.Token,
                            Permissions.Endpoints.Introspection,
                            Permissions.GrantTypes.Password,
                            Permissions.GrantTypes.RefreshToken,

                            Permissions.Prefixes.Scope + "IdentityScope",
                            Permissions.Prefixes.Scope + "FCNScope",
                            Permissions.Prefixes.Scope + "MembershipScope",
                        },
                        Requirements =
                        {
                            Requirements.Features.ProofKeyForCodeExchange
                        }
                    });
                }

                //if (await applicationManager.FindByClientIdAsync("server_api_2") == null)
                //{
                //    await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                //    {
                //        ClientId = "server_api_2",
                //        ClientSecret = "61973602-F664-41EE-846F-AFDCF65865DC",
                //        DisplayName = "Server Api 2",
                //        ConsentType = ConsentTypes.Explicit,
                //        Type = ClientTypes.Confidential,
                //        Permissions =
                //        {
                //            Permissions.Endpoints.Token,
                //            Permissions.Endpoints.Introspection,
                //            Permissions.GrantTypes.Password,
                //            Permissions.GrantTypes.RefreshToken,

                //            Permissions.Prefixes.Scope + "MembershipScope",
                //            Permissions.Prefixes.Scope + "FCNScope"
                //        },
                //        Requirements =
                //        {
                //            Requirements.Features.ProofKeyForCodeExchange
                //        }
                //    });
                //}

                // Add scopes
                var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
                if (await scopeManager.FindByNameAsync("IdentityScope") == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = "IdentityScope",
                        Resources = { "resource_identity_api" }
                    });
                }

                if (await scopeManager.FindByNameAsync("MembershipScope") == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = "MembershipScope",
                        Resources = { "resource_membership_api" }
                    });
                }

                if (await scopeManager.FindByNameAsync("FCNScope") == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = "FCNScope",
                        Resources = { "resource_fcn_api" }
                    });
                }

                //var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
                //var authorizations = await authorizationManager.FindByApplicationIdAsync("bee7ef3c-8b9b-4da8-a2ef-f907281af47d").ToListAsync();
                //if (!authorizations.Any())
                //{
                //    var principal = new System.Security.Claims.ClaimsPrincipal();
                //    await authorizationManager.CreateAsync(principal, "bee7ef3c-8b9b-4da8-a2ef-f907281af47d", 
                //        "bee7ef3c-8b9b-4da8-a2ef-f907281af47d", 
                //        AuthorizationTypes.Permanent, 
                //        ImmutableArray.Create("MembershipScope", "FCNScope"));
                //}
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
