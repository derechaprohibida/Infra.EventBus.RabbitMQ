using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Infra.EventBus.RabbitMQ
{
  public class EventBusRabbitMQ : IEventBus
  {
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly int _retryCount;
    private readonly RabbitMQOptions _options;
    private IModel channel;

    public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection, ILogger<EventBusRabbitMQ> logger,  IOptions<RabbitMQOptions> options)
    {
      _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _options = options.Value;
    }

    public void Publish(object @event)
    {
      if (!_persistentConnection.IsConnected)
      {
        _persistentConnection.TryConnect();
      }

      var policy = RetryPolicy.Handle<BrokerUnreachableException>()
          .Or<SocketException>()
          .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
          {
            _logger.LogWarning(ex, "Could not publish event: after {Timeout}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
          });

      var eventName = @event.GetType().Name;

      //_logger.LogInformation("Creating RabbitMQ channel to publish event: ({EventName})", eventName);
      /// Probar abriendo el channel todo el tiempo
      if(channel == null || channel.IsClosed)
      {
        _logger.LogWarning("Creating Channel");
        channel = CreatePublisherChannel();
      }
      //using (var channel = _persistentConnection.CreateModel())
      //{
        
        _logger.LogTrace("Declaring RabbitMQ exchange to publish event");


                                
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));
               
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Persistent = _options.Persistent;

        _logger.LogTrace("Publishing event to RabbitMQ");
                 
        channel.BasicPublish(
            exchange: _options.Exchange.Name,
            routingKey: (_options.UseEventNameAsRoutingKey ? eventName : string.Empty),
            mandatory: true,
            basicProperties: properties,
            body: body);

      //}
    }

    private IModel CreatePublisherChannel()
    {

      _logger.LogTrace("Declaring RabbitMQ exchange to publish event");

      var channel = _persistentConnection.CreateModel();
      channel.ExchangeDeclare(exchange: _options.Exchange.Name, type: _options.Exchange.Kind, durable: _options.Exchange.Durable);

      channel.CallbackException += (sender, ea) =>
      {
        _logger.LogWarning(ea.Exception, "Recreating RabbitMQ publisher channel");
        channel.Dispose();
        channel = CreatePublisherChannel();
      };

      return channel;
    }
  }
}
