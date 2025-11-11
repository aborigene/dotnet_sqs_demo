namespace SqsDemo.Services;

public interface ISqsService
{
    Task<string> SendMessageAsync(string id);
}
