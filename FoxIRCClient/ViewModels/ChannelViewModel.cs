using FoxIRCClient.Utils;
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

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                if (_isSelected)
                    HasUnread = false;
            }
        }
    }

    private bool _hasUnread;
    public bool HasUnread
    {
        get => _hasUnread;
        set => SetProperty(ref _hasUnread, value);
    }

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

            if (text.IndexOf('\n') == -1)
                _parentServer.Client.SendMessageAsync(ChannelId, text);
            else
                foreach (var line in text.Split('\n'))
                    _parentServer.Client.SendMessageAsync(ChannelId, line);

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

    public void AddMessage(string author, string message)
    {
        Application.Current.Dispatcher.Invoke(() => Messages.Add(new ChatMessageViewModel(DateTime.Now, author, message)));
        if (ChannelId != "System" && !IsSelected)
            HasUnread = true;
    }
    public void ClearMessages() =>
        Application.Current.Dispatcher.Invoke(() => Messages.Clear());
}
