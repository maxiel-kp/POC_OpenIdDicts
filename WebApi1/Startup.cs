using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Validation.OpenIddictValidationEvents;

namespace WebApi1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            services.AddOpenIddict().AddValidation(options =>
            {
                #region For Client credentials flow
                //// Note: the validation handler uses OpenID Connect discovery
                //// to retrieve the address of the introspection endpoint.
                //options.SetIssuer("https://localhost:44328/");
                //options.AddAudiences("server_api_1");

                //// Configure the validation handler to use introspection and register the client
                //// credentials used when communicating with the remote introspection endpoint.
                //options.UseIntrospection()
                //       .SetClientId("server_api_1")
                //       .SetClientSecret("388D45FA-B36B-4988-BA59-B187D329C207");
                #endregion

                #region For Password credentials flow
                options.SetIssuer("https://localhost:44363/");
                options.AddAudiences("resource_identity_api");

                options.UseIntrospection()
                       .SetClientId("server_api_1")
                       .SetClientSecret("07EA7AF4-32E4-4D35-B1AD-A94F7C3E0B43")
                       .AddEventHandler<HandleIntrospectionResponseContext>(config =>
                       {
                           config.UseSingletonHandler<ValidateTokenIntrospect>();
                       });
                #endregion

                // Register the System.Net.Http integration.
                options.UseSystemNetHttp();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseHsts();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });

        }
    }

    public class ValidateTokenIntrospect : IOpenIddictValidationHandler<HandleIntrospectionResponseContext>
    {
        public ValueTask HandleAsync(HandleIntrospectionResponseContext context)
        {
            return default;
        }
    }
}
