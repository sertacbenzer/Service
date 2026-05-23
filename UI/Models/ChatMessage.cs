namespace UI.Models;

public class ChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
