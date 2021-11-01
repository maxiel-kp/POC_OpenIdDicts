using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Validation.AspNetCore;

namespace WebApi2
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
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

            services.AddOpenIddict().AddValidation(options =>
            {
                options.SetIssuer("https://localhost:44363/");
                options.AddAudiences("resource_fcn_api", "resource_membership_api");
            
                options.UseIntrospection()
                       .SetClientId("server_api_1")
                       .SetClientSecret("07EA7AF4-32E4-4D35-B1AD-A94F7C3E0B43");

                //options.UseIntrospection()
                //       .SetClientId("server_api_2")
                //       .SetClientSecret("61973602-F664-41EE-846F-AFDCF65865DC");

                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
