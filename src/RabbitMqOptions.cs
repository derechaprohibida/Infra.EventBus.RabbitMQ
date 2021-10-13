
namespace Infra.EventBus.RabbitMQ
{
  public class RabbitMQOptions
  {
    public string HostName { get; set; }
    public string QueueName { get; set; }
    public Exchange Exchange { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public int? RetryCount { get; set; } = 3;
    public bool Persistent { get; set; } = true;
    public bool UseEventNameAsRoutingKey { get; set; } = false;
  }

  public class Exchange
  {
    public string Name { get; set; }
    public string Kind { get; set; } = "direct";
    public bool Durable { get; set; } = true;
  }
}
