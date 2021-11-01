using Autofac;
using IC.Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PasswordCredentials.Server.Data;

namespace PasswordCredentials.Server.Modules
{
    public class EFModule : Module
    {
        /// <inheritdoc/>
        public IConfiguration Configuration { get; private set; }

        /// <inheritdoc/>
        public EFModule(IConfiguration configuration) : base()
        {
            Configuration = configuration;
        }

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();

            builder.Register((com, p) =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                string connectionString = Configuration.GetConnectionString("LocalDb");

                optionsBuilder.UseOpenIddict();// Use OpendIddict.

                return optionsBuilder.UseSqlServer(connectionString, builder =>
                {
                    builder.EnableRetryOnFailure();
                    builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
                }).Options;
            }).SingleInstance();

            builder.Register((com, p) =>
            {
                var contextOptions = com.Resolve<DbContextOptions<ApplicationDbContext>>();
                return new ApplicationDbContext(contextOptions);
            }).As<IDbContext>().As<ApplicationDbContext>()
            .InstancePerLifetimeScope();

        }
    }
}
