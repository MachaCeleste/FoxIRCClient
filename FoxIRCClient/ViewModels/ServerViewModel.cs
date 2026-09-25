using FoxIrc;
using FoxIRCClient.Utils;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace FoxIRCClient.ViewModels;

public class ServerViewModel : ViewModelBase
{
    public string ServerAddress { get; set; }
    public int Port { get; set; }
    public string Nick { get; set; }
    public string User { get; set; }
    public string Real { get; set; }
    public string? Pass { get; set; }

    public IrcClient Client { get; private set; }
    public ObservableCollection<ChannelViewModel> Channels { get; } = [];

    private bool _hasUnread;
    public bool HasUnread
    {
        get => _hasUnread;
        set => SetProperty(ref _hasUnread, value);
    }

    private ChannelViewModel? _selectedChannel;
    public ChannelViewModel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (_selectedChannel != value)
            {
                if (_selectedChannel != null)
                    _selectedChannel.IsSelected = false;
                _selectedChannel = value;
                OnPropertyChanged(nameof(SelectedChannel));
                if (_selectedChannel != null)
                {
                    _selectedChannel.IsSelected = true;
                    _selectedChannel.HasUnread = false;
                }
            }
        }
    }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand CloseChannelCommand { get; }

    public ServerViewModel(string server, string nick, string user, string real, string? pass = null, int port = 6667)
    {
        ServerAddress = server;
        Port = port;
        Nick = nick;
        User = user;
        Real = real;
        Pass = pass;

        Client = new IrcClient(ServerAddress, Nick, User, Real, Pass, Port);
        Client.RawDataReceived += (client, rawLine) => RawMessageReceivedHandler.ProcessMessage(this, rawLine);
        Client.SystemLog += OnSystemLog;

        CloseChannelCommand = new RelayCommand(OnCloseChannel);
        ConnectCommand = new RelayCommand(async () => await ConnectNetworkAsync(), () => !Client.IsConnected);
        DisconnectCommand = new RelayCommand(async () => await DisconnectNetworkAsync(), () => Client.IsConnected);

        Channels.CollectionChanged += Channels_CollectionChanged;

        GetOrCreateChannel("System");
    }

    private void Channels_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ChannelViewModel channel in e.NewItems)
                channel.PropertyChanged += Channel_PropertyChanged;

        if (e.OldItems != null)
            foreach (ChannelViewModel channel in e.OldItems)
                channel.PropertyChanged -= Channel_PropertyChanged;

        UpdateUnreadStatus();
    }

    private void Channel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChannelViewModel.HasUnread))
            UpdateUnreadStatus();
    }

    private void UpdateUnreadStatus() =>
        HasUnread = Channels.Any(x => x.HasUnread);

    public async Task ConnectNetworkAsync()
    {
        if (Client.IsConnected) return;

        try
        {
            GetOrCreateChannel("System").AddMessage("SYSTEM", $"Connecting to {ServerAddress}:{Port}...");
            await Client.ConnectAsync();
        }
        catch (Exception ex)
        {
            GetOrCreateChannel("System").AddMessage("ERROR", $"Connection failed: {ex.Message}");
        }
    }

    public async Task DisconnectNetworkAsync()
    {
        if (!Client.IsConnected) return;

        await Client.DisconnectAsync("User Disconnected");
        GetOrCreateChannel("System").AddMessage("SYSTEM", "Session terminated.");
    }

    private void OnSystemLog(IrcClient client, string logMessage)
    {
        ChannelViewModel sysChannel = GetOrCreateChannel("System");
        sysChannel.AddMessage("SYSTEM", logMessage);
    }

    public ChannelViewModel? GetChannel(string channelId) =>
        Channels.FirstOrDefault(x => x.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));

    public ChannelViewModel GetOrCreateChannel(string channelId)
    {
        var channel = GetChannel(channelId);
        if (channel == null)
        {
            channel = new ChannelViewModel(this, channelId);
            Application.Current.Dispatcher.Invoke(() => Channels.Add(channel));
        }
        return channel;
    }

    public void CloseChannel(ChannelViewModel channel)
    {
        if (channel == null || channel.ChannelId.Equals("System", StringComparison.OrdinalIgnoreCase))
            return;

        if (Client.IsConnected && channel.ChannelId.StartsWith('#'))
            _ = Client.LeaveChannelAsync(channel.ChannelId);

        Application.Current.Dispatcher.Invoke(() => Channels.Remove(channel));
    }

    public async void OnCloseChannel(object parameter)
    {
        if (parameter is ChannelViewModel channel)
            CloseChannel(channel);
    }
}
