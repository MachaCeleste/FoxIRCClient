using FoxIRCClient.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace FoxIRCClient.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Fields
    public ObservableCollection<ServerViewModel> Servers { get; } = [];

    private bool _isAddingServerOpen;
    public bool IsAddingServerOpen
    {
        get => _isAddingServerOpen;
        set => SetProperty(ref _isAddingServerOpen, value);
    }

    private string? _newServerAddress;
    public string? NewServerAddress
    {
        get => _newServerAddress;
        set => SetProperty(ref _newServerAddress, value);
    }

    private string _newPort = "6667";
    public string NewPort
    {
        get => _newPort;
        set => SetProperty(ref _newPort, value);
    }

    private string? _newNick;
    public string? NewNick
    {
        get => _newNick;
        set => SetProperty(ref _newNick, value);
    }

    private string _newUser = "FoxIrcUser";
    public string NewUser
    {
        get => _newUser;
        set => SetProperty(ref _newUser, value);
    }

    private string _newReal = "FoxIrcReal";
    public string NewReal
    {
        get => _newReal;
        set => SetProperty(ref _newReal, value);
    }

    private string? _newPass;
    public string? NewPass
    {
        get => _newPass;
        set => SetProperty(ref _newPass, value);
    }


    private bool _isConfirmDeleteOpen;
    public bool IsConfirmDeleteOpen
    {
        get => _isConfirmDeleteOpen;
        set => SetProperty(ref _isConfirmDeleteOpen, value);
    }

    private ServerViewModel? _serverToDelete;
    public ServerViewModel? ServerToDelete
    {
        get => _serverToDelete;
        set => SetProperty(ref _serverToDelete, value);
    }

    public ObservableCollection<string> CustomThemes => ThemeManager.CustomThemes;

    public bool HasThemes => CustomThemes.Count > 0;
    #endregion

    #region Commands
    public ICommand ConnectAllCommand { get; }
    public ICommand CloseApplicationCommand { get; }

    public ICommand OpenAddServerPanelCommand { get; }
    public ICommand ConfirmAddServerCommand { get; }
    public ICommand CancelAddServerCommand { get; }

    public ICommand PromptDeleteServerCommand { get; }
    public ICommand ConfirmDeleteServerCommand { get; }
    public ICommand CancelDeleteServerCommand { get; }

    public ICommand OpenThemesFolderCommand { get; }
    public ICommand ChangeThemeCommand { get; }
    public ICommand ReloadCustomThemesCommand { get; }
    #endregion

    public MainViewModel()
    {
        IsAddingServerOpen = true;

        List<ServerViewModel>? loadServers = DataManager.LoadServerConfigs();
        if (loadServers != null)
        {
            Servers = [..loadServers];
            if (Servers.Count > 0)
                IsAddingServerOpen = false;
        }

        DataManager.LoadThemeConfig();

        ConnectAllCommand = new RelayCommand(async () => await ExecuteConnectAllAsync());
        CloseApplicationCommand = new RelayCommand(() => Application.Current.Shutdown());

        OpenAddServerPanelCommand = new RelayCommand(() => IsAddingServerOpen = true);
        CancelAddServerCommand = new RelayCommand(() => IsAddingServerOpen = false, () => Servers.Count > 0);
        ConfirmAddServerCommand = new RelayCommand(ExecuteAddServer, CanAddServer);

        PromptDeleteServerCommand = new RelayCommand(OnCloseServer);
        ConfirmDeleteServerCommand = new RelayCommand(ConfirmDeleteServer);
        CancelDeleteServerCommand = new RelayCommand(CancelDeleteServer);

        ThemeManager.ReloadCustomThemes();
        OpenThemesFolderCommand = new RelayCommand(() => ThemeManager.OpenThemesFolder());
        CustomThemes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasThemes));
        ReloadCustomThemesCommand = new RelayCommand(() => ThemeManager.ReloadCustomThemes());
        ChangeThemeCommand = new RelayCommand((param) =>
        {
            if (param is string themeName && !string.IsNullOrEmpty(themeName))
                ThemeManager.ApplyTheme(themeName);
        });
    }

    private async Task ExecuteConnectAllAsync()
    {
        foreach (var server in Servers)
            if (server.ConnectCommand.CanExecute(null))
                server.ConnectCommand.Execute(null);
    }

    #region Add Server
    private bool CanAddServer() =>
        !string.IsNullOrWhiteSpace(NewServerAddress) && !string.IsNullOrWhiteSpace(NewNick);

    private void ExecuteAddServer()
    {
        if (!CanAddServer()) return;

        int.TryParse(NewPort, out int port);
        if (port <= 1023) port = 6667;

        var server = new ServerViewModel(NewServerAddress!.Trim(), NewNick!.Trim(), NewUser.Trim(), NewReal.Trim(), NewPass, port);

        Servers.Add(server);
        DataManager.SaveServerConfigs([..Servers]);
        ResetForm();
        IsAddingServerOpen = false;
    }

    private void ResetForm()
    {
        NewServerAddress = string.Empty;
        NewNick = string.Empty;
        NewPort = "6667";
        NewUser = "FoxIrcUser";
        NewReal = "FoxIrcReal";
        NewPass = null;
    }
    #endregion

    #region Delete Server
    private async void OnCloseServer(object parameter)
    {
        if (parameter is ServerViewModel server)
        {
            if (server == null) return;
            ServerToDelete = server;
            IsConfirmDeleteOpen = true;
        }
    }

    private async void ConfirmDeleteServer()
    {
        if (ServerToDelete != null)
        {
            await ServerToDelete.DisconnectNetworkAsync();
            Application.Current.Dispatcher.Invoke(() => Servers.Remove(ServerToDelete));
            DataManager.SaveServerConfigs([.. Servers]);
            ServerToDelete = null;
        }
        IsConfirmDeleteOpen = false;
    }

    private void CancelDeleteServer()
    {
        ServerToDelete = null;
        IsConfirmDeleteOpen= false;
    }
    #endregion
}
