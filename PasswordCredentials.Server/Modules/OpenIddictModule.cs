using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using PasswordCredentials.Server.Data;
using PasswordCredentials.Server.Handlers;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace PasswordCredentials.Server.Modules
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenIddictModule : Module
    {
        /// <summary>
        /// 
        /// </summary>
        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();

            services.AddIdentity<UserAccount, IdentityRole>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = Claims.Role;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
            // (like pruning orphaned authorizations/tokens from the database) at regular intervals.
            services.AddQuartz(options =>
            {
                options.UseMicrosoftDependencyInjectionJobFactory();
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            services.AddOpenIddict()
                    // Register the OpenIddict core components.
                    .AddCore(options =>
                    {
                        // Configure OpenIddict to use the Entity Framework Core stores and models.
                        // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                        options.UseEntityFrameworkCore()
                               .UseDbContext<ApplicationDbContext>();

                        // Enable Quartz.NET integration.
                        options.UseQuartz();
                    })
                    // Register the OpenIddict server components.
                    .AddServer(options =>
                    {
                        // Enable the token endpoint.
                        options.SetTokenEndpointUris("/api/v1.0/connect/token")
                               .SetIntrospectionEndpointUris("/connect/introspect");

                        // Mark the "email", "profile" and "roles" scopes as supported scopes.
                        options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                        // Enable the client credentials flow.
                        options.AllowPasswordFlow().AllowRefreshTokenFlow();

                        // Accept anonymous clients (i.e clients that don't send a client_id).
                        options.AcceptAnonymousClients();

                        options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(5))
                               .SetRefreshTokenReuseLeeway(TimeSpan.FromMinutes(5));

                        // Register the signing and encryption credentials.
                        options.AddDevelopmentEncryptionCertificate()
                               .AddDevelopmentSigningCertificate();

                        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                        options.UseAspNetCore()
                               .EnableTokenEndpointPassthrough()
                               .EnableStatusCodePagesIntegration();

                        //options.DisableAccessTokenEncryption();
                        options.UseDataProtection()
                               .PreferDefaultAccessTokenFormat()
                               .PreferDefaultRefreshTokenFormat();

                        options.UseReferenceAccessTokens()
                               .UseReferenceRefreshTokens();

                        options.AddEventHandler<ApplyTokenResponseContext>(builder =>
                        {
                            builder.UseSingletonHandler<OpenIddictResponseHandler>();
                        });

                        options.AddEventHandler<ApplyIntrospectionResponseContext>(builder =>
                        {
                            builder.UseSingletonHandler<CustomizeApplyIntrospection>();
                        });

                        options.AddEventHandler<HandleIntrospectionRequestContext>(builder =>
                        {
                            builder.UseSingletonHandler<CustomizeHandleIntrospectionRequestContext>();
                        });

                    })
                    // Register the OpenIddict validation components.
                    .AddValidation(options =>
                    {
                        // Validate token.
                        options.EnableTokenEntryValidation();

                        // Import the configuration from the local OpenIddict server instance.
                        options.UseLocalServer();

                        // Register the ASP.NET Core host.
                        options.UseAspNetCore();
                    });

            builder.Populate(services);
        }
    }

    public class CustomizeApplyIntrospection : IOpenIddictServerHandler<ApplyIntrospectionResponseContext>
    {
        public ValueTask HandleAsync(ApplyIntrospectionResponseContext context)
        {

            return default;
        }
    }

    public class CustomizeHandleIntrospectionRequestContext : IOpenIddictServerHandler<HandleIntrospectionRequestContext>
    {
        public ValueTask HandleAsync(HandleIntrospectionRequestContext context)
        {
            if (context.Principal.Claims.Any())
            {
                var userClaim = context.Principal.Claims.FirstOrDefault(w => w.Type == Claims.Name);
                context.Username = userClaim?.Value;
                context.Claims.Add(Claims.Name, userClaim?.Value);

                var roles = context.Principal.Claims
                                   .Where(w => w.Type == Claims.Role).Select(s => s.Value)
                                   .ToArray();
                if (roles.Any())
                {
                    context.Claims.Add(Claims.Role, new OpenIddictParameter(roles));
                }
            }

            return default;
        }

        //private Claim GetEmailClaim(HandleIntrospectionRequestContext context)
        //{
        //    var emailClaim = context.Principal.Claims.FirstOrDefault(w => w.Type == Claims.Email);
        //    if (emailClaim == null)
        //    {
        //        //emailClaim = context.Principal.Claims.FirstOrDefault(w=>w.Type == )
        //    }
        //}

    }

}
