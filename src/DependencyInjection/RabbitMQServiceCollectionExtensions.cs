using Microsoft.Extensions.Configuration;
using Infra.EventBus.RabbitMQ;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection
{
  public static class RabbitMQServiceCollectionExtensions
  {
    public static void AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration, string configSectionName = "RabbitMQ")
    {
      AddDefaultRabbitMQPersistentConnection(services, configuration, configSectionName);
      services.AddSingleton<IEventBus, EventBusRabbitMQ>();
    }

    public static void AddDefaultRabbitMQPersistentConnection(this IServiceCollection services, IConfiguration configuration, string configSectionName)
    {
      services.Configure<RabbitMQOptions>(options => configuration.GetSection(configSectionName).Bind(options));
      services.AddSingleton<IConnectionFactory, ConnectionFactory>();
      services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
    }
  }
}
