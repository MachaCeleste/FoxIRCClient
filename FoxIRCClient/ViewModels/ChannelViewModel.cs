using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace FoxIRCClient.ViewModels;

public class ChannelViewModel : ViewModelBase
{
    private readonly ServerViewModel _parentServer;

    public string ChannelId { get; private set; }
    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];
    public ObservableCollection<UserViewModel> Users { get; } = [];

    private string _inputText = string.Empty;
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public ICommand SendMessageCommand { get; }

    public ChannelViewModel(ServerViewModel parentServer, string channelId)
    {
        _parentServer = parentServer;
        ChannelId = channelId;
        SendMessageCommand = new RelayCommand(ExecuteSendMessage, CanSendMessage);
    }

    private void ExecuteSendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        string text = InputText.Trim();
        InputText = string.Empty;

        if (text.StartsWith('/'))
        {
            SlashCommandHandler.HandleCommand(_parentServer, this, text);
            return;
        }

        if (_parentServer.Client.IsConnected)
        {
            if (ChannelId.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                AddMessage("SYSTEM", "Cannot send messages in System. Use /commands for a list of available commands.");
                return;
            }

            _parentServer.Client.SendMessageAsync(ChannelId, text);
            AddMessage(_parentServer.Nick, text);
        }
        else
            AddMessage("SYSTEM", "Not connected to server!");
    }

    private bool CanSendMessage() => !string.IsNullOrWhiteSpace(InputText);

    public void AddUser(UserViewModel user) =>
        Application.Current.Dispatcher.Invoke(() => Users.Add(user));
    public void RemoveUser(UserViewModel user) =>
        Application.Current.Dispatcher.Invoke(() => Users.Remove(user));
    public UserViewModel? GetUserByNick(string nick) =>
        Users.FirstOrDefault(x => x.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));
    public void ClearUsers() =>
        Application.Current.Dispatcher.Invoke(() => Users.Clear);

    public void AddMessage(string author, string message) =>
        Application.Current.Dispatcher.Invoke(() => Messages.Add(new ChatMessageViewModel(DateTime.Now, author, message)));
    public void ClearMessages() =>
        Application.Current.Dispatcher.Invoke(() => Messages.Clear);
}
