namespace KafkaDemo.Services;

public interface IKafkaService
{
    Task<string> SendMessageAsync(string id);
}
