namespace  Infra.EventBus.RabbitMQ
{
    public interface IEventBus
    {
        void Publish(object message);
    }
}