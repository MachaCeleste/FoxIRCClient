namespace FoxIRCClient.ViewModels;

public class UserViewModel(string nick, string? user = null, string? real = null) : ViewModelBase
{
    public string Nick { get; set; } = nick;
    public string Username { get; set; } = user ?? string.Empty;
    public string Realname { get; set; } = real ?? string.Empty;
}
