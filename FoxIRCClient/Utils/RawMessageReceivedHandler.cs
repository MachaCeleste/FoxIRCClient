using FoxIRCClient.ViewModels;
using System.Windows;

namespace FoxIRCClient.Utils;

public static class RawMessageReceivedHandler
{
    public static void ProcessMessage(ServerViewModel server, string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage)) return;

        string line = rawMessage;
        string prefix = string.Empty;

        if (line.StartsWith(':'))
        {
            int spaceIdx = line.IndexOf(' ');
            if (spaceIdx == -1) return;
            prefix = line[1..spaceIdx];
            line = line[(spaceIdx + 1)..];
        }

        string command;
        int cmdSpaceIdx = line.IndexOf(' ');
        if (cmdSpaceIdx == -1)
        {
            command = line;
            line = string.Empty;
        }
        else
        {
            command = line[..cmdSpaceIdx];
            line = line[(cmdSpaceIdx + 1)..];
        }

        string paramsStr = line;
        string trailing = string.Empty;

        int trailingIdx = line.IndexOf(" :");
        if (trailingIdx != -1)
        {
            paramsStr = line[..trailingIdx];
            trailing = line[(trailingIdx + 2)..];
        }
        else if (line.StartsWith(':'))
        {
            paramsStr = string.Empty;
            trailing = line[1..];
        }

        string[] parameters = paramsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string senderNick = prefix.Contains('!') ? prefix[..prefix.IndexOf('!')] : prefix;

        switch (command.ToUpperInvariant())
        {
            case "PRIVMSG":
                if (parameters.Length > 0)
                {
                    string target = parameters[0];
                    string channelKey = target.Equals(server.Client.Nick, StringComparison.OrdinalIgnoreCase) ? senderNick : target;

                    ChannelViewModel channel = server.GetOrCreateChannel(channelKey);
                    channel.AddMessage(senderNick, trailing);
                }
                break;

            case "NOTICE":
                {
                    string target = parameters.Length > 0 ? parameters[0] : "System";
                    string channelKey = target.Equals(server.Client.Nick, StringComparison.OrdinalIgnoreCase) || target == "*" ? "System" : target;

                    ChannelViewModel channel = server.GetOrCreateChannel(channelKey);
                }
                break;

            case "JOIN":
                {
                    string chanName = parameters.Length > 0 ? parameters[0] : trailing;
                    ChannelViewModel channel = server.GetOrCreateChannel(chanName);

                    channel.AddMessage("SYSTEM", $"{senderNick} joined {chanName}");

                    if (!channel.Users.Any(x => x.Nick.Equals(senderNick, StringComparison.OrdinalIgnoreCase)))
                        channel.AddUser(new UserViewModel(senderNick));
                }
                break;

            case "PART":
                {
                    string chanName = parameters.Length > 0 ? parameters[0] : string.Empty;
                    if (!string.IsNullOrEmpty(chanName))
                    {
                        ChannelViewModel? channel = server.GetChannel(chanName);
                        string reason = string.IsNullOrEmpty(trailing) ? "" : $" ({trailing})";
                        string logMessage = $"{senderNick} left {chanName}: {reason}";

                        if (channel != null)
                        {
                            channel.AddMessage("SYSTEM", logMessage);
                            RemoveUserFromChannel(channel, senderNick);

                            if (senderNick.Equals(server.Nick, StringComparison.OrdinalIgnoreCase))
                                server.CloseChannel(channel);
                        }
                        else
                        {
                            ChannelViewModel sysChannel = server.GetChannel("System") ?? server.GetOrCreateChannel("System");
                            sysChannel.AddMessage("SYSTEM", logMessage);
                        }
                    }
                }
                break;

            case "KICK":
                if (parameters.Length >= 2)
                {
                    string chanName = parameters[0];
                    string kickedUser = parameters[1];
                    ChannelViewModel channel = server.GetOrCreateChannel(chanName);

                    channel.AddMessage("SYSTEM", $"{senderNick} kicked {kickedUser}: {trailing}");
                    RemoveUserFromChannel(channel, kickedUser);
                }
                break;

            case "QUIT":
                {
                    string reason = string.IsNullOrEmpty(trailing) ? "" : $" ({trailing})";
                    foreach (var channel in server.Channels)
                    {
                        if (channel.Users.Any(x => x.Nick.Equals(reason, StringComparison.OrdinalIgnoreCase)))
                        {
                            channel.AddMessage("SYSTEM", $"{senderNick} quit: {reason}");
                            RemoveUserFromChannel(channel, senderNick);
                        }
                    }
                }
                break;

            case "NICK":
                {
                    string newNick = trailing.Length > 0 ? trailing : (parameters.Length > 0 ? parameters[0] : string.Empty);
                    if (!string.IsNullOrEmpty(newNick))
                    {
                        foreach (var channel in server.Channels)
                        {
                            var userNode = channel.Users.FirstOrDefault(x => x.Username.Equals(senderNick, StringComparison.OrdinalIgnoreCase));
                            if (userNode != null)
                            {
                                channel.AddMessage("SYSTEM", $"{senderNick} is now known as {newNick}");
                                Application.Current.Dispatcher.Invoke(() => userNode.Nick = newNick);
                            }
                        }
                    }
                }
                break;

            case "301": // RPL_AWAY
                if (parameters.Length >= 2)
                {
                    string awayNick = parameters[1];
                    string awayMessage = string.IsNullOrEmpty(trailing) ? "Away" : trailing;

                    ChannelViewModel sysChannel = server.GetOrCreateChannel("System");
                    sysChannel.AddMessage("SYSTEM", $"{awayNick} is away: {awayMessage}");
                }
                break;

            case "305": // RPL_UNAWAY
            case "306": // RPL_NOWAWAY
                {
                    ChannelViewModel sysChannel = server.GetOrCreateChannel("System");
                    string sysMsg = string.IsNullOrEmpty(trailing) ? "Away status updated." : trailing;
                    sysChannel.AddMessage("SERVER", sysMsg);
                }
                break;

            case "322": // RPL_LIST
                if (parameters.Length >= 3)
                {
                    string listChan = parameters[1];
                    string userCount = parameters[2];
                    ChannelViewModel sysChannel = server.GetOrCreateChannel("System");

                    sysChannel.AddMessage("LIST", $"{listChan} (Users: {userCount}) - {trailing}");
                }
                break;

            case "353": // RPL_NAMREPLY
                if (parameters.Length >= 3)
                {
                    string chanName = parameters[2];
                    ChannelViewModel channel = server.GetOrCreateChannel(chanName);

                    string[] userList = trailing.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    foreach (string rawUser in userList)
                    {
                        string cleanNick = rawUser.TrimStart('@', '+', '%', '&', '~');

                        if (!channel.Users.Any(x => x.Nick.Equals(cleanNick, StringComparison.OrdinalIgnoreCase)))
                            channel.AddUser(new UserViewModel(cleanNick));
                    }
                }
                break;

            case "332": // RPL_TOPIC
                if (parameters.Length >= 2)
                {
                    string chanName = parameters[1];
                    ChannelViewModel channel = server.GetOrCreateChannel(chanName);
                    channel.AddMessage("TOPIC", trailing);
                }
                break;

            case "001": // RPL_WELCOME
            case "002": // RPL_YOURHOST
            case "003": // RPL_CREATED
            case "004": // RPL_MYINFO
            case "005": // RPL_ISUPPORT
            case "372": // RPL_MOTD
            case "375": // RPL_MOTDSTART
            case "376": // RPL_ENDOFMOTD
                {
                    ChannelViewModel sysChannel = server.GetOrCreateChannel("System");
                    string sysMsg = string.IsNullOrEmpty(trailing) ? string.Join(' ', parameters) : trailing;
                    sysChannel.AddMessage("SERVER", sysMsg);
                }
                break;

            default:
                if (!string.IsNullOrEmpty(trailing))
                {
                    ChannelViewModel sysChannel = server.GetOrCreateChannel("System");
                    sysChannel.AddMessage("SYSTEM", $"[{command}] {trailing}");
                }
                break;
        }
    }

    private static void RemoveUserFromChannel(ChannelViewModel channel, string nick)
    {
        UserViewModel? user = channel.GetUserByNick(nick);
        if (user != null)
            channel.RemoveUser(user);
    }
}
