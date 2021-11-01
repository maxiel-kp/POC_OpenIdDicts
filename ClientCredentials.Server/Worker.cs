using System;
using System.Threading;
using System.Threading.Tasks;
using ClientCredentials.Server.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ClientCredentials.Server
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
                        ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
                        DisplayName = "Server Api 1",
                        ConsentType = ConsentTypes.Explicit,
                        Type = ClientTypes.Confidential,
                        Permissions =
                        {
                            Permissions.Endpoints.Token,
                            Permissions.Endpoints.Introspection,
                            Permissions.GrantTypes.ClientCredentials,
                            Permissions.GrantTypes.RefreshToken,
                            Permissions.Prefixes.Scope + "api1"
                        },
                        Requirements = 
                        {
                            Requirements.Features.ProofKeyForCodeExchange
                        }
                    });
                }

                //With Scope
                var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
                if (await scopeManager.FindByNameAsync("api1") == null)
                {
                    await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                    {
                        Name = "api1",
                        Resources = { "server_api_1" }
                    });
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
