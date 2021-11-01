using Autofac;
using IC.Common.Web.DependencyInjection;
using IC.Common.Web.Helpers;
using IC.Common.Web.Swaggers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PasswordCredentials.Server.Data;
using PasswordCredentials.Server.Modules;

namespace PasswordCredentials.Server
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup : BaseStartup, IAutofacStartup
    {
        /// <summary>
        /// 
        /// </summary>
        public Startup(IConfiguration configuration) : base(typeof(Startup))
        {
            Configuration = configuration;
        }

        //public IConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            CommonConfigureServices(services);

            services.AddAutoMapper(CurrentAssembly);
            services.AddSwaggerDocuments(AssemblyName);
            services.RegisterExpcetionFilter();

            services.AddHostedService<Worker>();

            services.AddControllers();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CommonConfigure(app, env);

            app.UseHttpsRedirection();
            app.UseHsts();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new EFModule(Configuration));
            builder.RegisterModule<OpenIddictModule>();
        }
    }
}
