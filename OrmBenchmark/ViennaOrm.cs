using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using ViennaNET.Mediator.DefaultConfiguration;
using ViennaNET.Orm;
using ViennaNET.Orm.DefaultConfiguration;
using ViennaNET.Orm.PostgreSql.DefaultConfiguration;
using ViennaNET.Orm.Seedwork;
using ViennaNET.SimpleInjector.Extensions;

namespace OrmBenchmark
{
    public class ViennaOrm
    {
        public static Container GetContainer()
        {
            var container = CreateServices();

            return container;
        }

        private static Container CreateServices()
        {
            var container = new Container();
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            container
                .AddPackage(new MediatorPackage())
                .AddPackage(new PostgreSqlOrmPackage())
                .AddPackage(new OrmPackage());

            container.Collection.Append<IBoundedContext, ViennaContext>(Lifestyle.Singleton);

            container.RegisterSingleton<IConfiguration>(() => new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build());
            container.Register(typeof(ILoggerFactory), () => new LoggerFactory(), Lifestyle.Singleton);
            container.RegisterSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            container.Verify();

            return container;
        }
    }

    public class ViennaContext : ApplicationContext
    {
        public ViennaContext()
        {
            AddEntity<Blog>();
        }
    }
}
