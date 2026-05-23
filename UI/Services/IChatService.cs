namespace UI.Services;

public interface IChatService
{
    Task<string> GetAnswerAsync(string question);
}
