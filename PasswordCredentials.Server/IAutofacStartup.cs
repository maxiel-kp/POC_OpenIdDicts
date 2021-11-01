using Autofac;

namespace PasswordCredentials.Server
{
    public interface IAutofacStartup
    {
        void ConfigureContainer(ContainerBuilder builder);
    }

}
