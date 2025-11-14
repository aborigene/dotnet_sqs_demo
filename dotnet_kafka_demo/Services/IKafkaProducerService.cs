namespace KafkaDemo.Services;

public interface IKafkaProducerService
{
    Task<string> SendMessageAsync(string id);
}
