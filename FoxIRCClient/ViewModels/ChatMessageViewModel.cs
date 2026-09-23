namespace FoxIRCClient.ViewModels;

public class ChatMessageViewModel(DateTime timeStamp, string nick, string message): ViewModelBase
{
    public DateTime Timestamp { get; set; } = timeStamp;
    public string Author { get; set; } = nick;
    public string Content { get; set; } = message;
}
