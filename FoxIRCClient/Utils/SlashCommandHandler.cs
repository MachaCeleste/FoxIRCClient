using FoxIrc;
using FoxIRCClient.ViewModels;

namespace FoxIRCClient.Utils;

public static class SlashCommandHandler
{
    public static void HandleCommand(ServerViewModel server, ChannelViewModel currentChannel, string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/')) return;

        string payload = input[1..].Trim();
        string[] parts = payload.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0].ToUpperInvariant();
        string args = parts.Length > 1 ? parts[1] : string.Empty;

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessCommandAsync(server, currentChannel, cmd, args, input);
            }
            catch (Exception ex)
            {
                currentChannel.AddMessage("SYSTEM", $"Command Error: {ex.Message}");
            }
        });
    }

    private static async Task ProcessCommandAsync(ServerViewModel server, ChannelViewModel currentChannel, string cmd, string args, string rawInput)
    {
        IrcClient client = server.Client;

        if (!server.Client.IsConnected && cmd != "HELP" && cmd != "COMMANDS")
        {
            currentChannel.AddMessage("SYSTEM", "Not connected to server!");
            return;
        }

        switch (cmd)
        {
            case "JOIN":
                if (!string.IsNullOrEmpty(args))
                    await client.JoinChannelAsync(args);
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /JOIN <#channel>");
                break;

            case "PART":
            case "LEAVE":
                string targetPart = string.IsNullOrEmpty(args) ? currentChannel.ChannelId : args;
                if (!targetPart.Equals("System", StringComparison.OrdinalIgnoreCase))
                {
                    var channelToClose = server.GetChannel(targetPart);
                    if (channelToClose != null)
                        server.CloseChannel(channelToClose);
                    else
                        await client.LeaveChannelAsync(targetPart);
                }
                else
                    currentChannel.AddMessage("SYSTEM", "Cannot leave the System channel.");
                break;

            case "TOPIC":
                if (currentChannel.ChannelId.Equals("System", StringComparison.OrdinalIgnoreCase))
                {
                    currentChannel.AddMessage("SYSTEM", "Specify channel to run /TOPIC");
                    return;
                }

                if (!string.IsNullOrEmpty(args))
                    await client.SetTopicAsync(currentChannel.ChannelId, args);
                else
                    await client.GetTopicAsync(currentChannel.ChannelId);
                break;

            case "LIST":
                if (string.IsNullOrEmpty(args))
                    await client.GetChannelsAsync();
                else
                    await client.GetChannelsAsync(args);
                break;

            case "ME":
                if (!string.IsNullOrEmpty(args))
                {
                    if (currentChannel.ChannelId.Equals("System", StringComparison.OrdinalIgnoreCase))
                    {
                        currentChannel.AddMessage("SYSTEM", "Cannot perform action in system tab.");
                        return;
                    }

                    string actionMsg = $"\u0001ACTION {args}\u0001";
                    await client.SendMessageAsync(currentChannel.ChannelId, actionMsg);
                    currentChannel.AddMessage("*", $"{server.Nick} {args}");
                }
                break;

            case "MSG":
                string[] msgParts = args.Split(' ', 2);
                if (msgParts.Length == 2)
                {
                    string target = msgParts[0];
                    string message = msgParts[1];

                    await client.SendMessageAsync(target, message);

                    ChannelViewModel targetLoc = server.GetOrCreateChannel(target);
                    targetLoc.AddMessage(server.Nick, message);
                }
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /MSG <target> <message>");
                break;

            case "NOTICE":
                string[] noticeParts = args.Split(' ', 2);
                if (noticeParts.Length == 2)
                {
                    await client.SendNoticeAsync(noticeParts[0], noticeParts[1]);
                    currentChannel.AddMessage("NOTICE", $"-> [{noticeParts[0]}] {noticeParts[1]}");
                }
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /NOTICE <target> <message>");
                break;

            case "AWAY":
                await client.AwayAsync(string.IsNullOrEmpty(args) ? null : args);
                break;

            case "WHOIS":
                if (!string.IsNullOrEmpty(args))
                {
                    string[] whoisArgs = args.Split(' ', 2);
                    if (whoisArgs.Length == 2)
                        await client.WhoisAsync(whoisArgs[0], whoisArgs[1]);
                    else
                        await client.WhoisAsync(whoisArgs[0]);
                }
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /WHOIS <nick> [target]");
                break;

            case "MOTD":
                await client.GetMotdAsync(string.IsNullOrEmpty(args) ? null : args);
                break;

            case "TIME":
                await client.GetServerTimeAsync(string.IsNullOrEmpty(args) ? null : args);
                break;

            case "ADMIN":
                await client.GetAdmin(string.IsNullOrEmpty(args) ? null : args);
                break;

            case "OPER":
                string[] operArgs = args.Split(' ', 2);
                if (operArgs.Length == 2)
                    await client.LoginOpAsync(operArgs[0], operArgs[1]);
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /OPER <username> <password>");
                break;

            case "KICK":
                if (currentChannel.ChannelId.Equals("System", StringComparison.OrdinalIgnoreCase))
                {
                    currentChannel.AddMessage("SYSTEM", "Execute /KICK inside a channel tab.");
                    return;
                }

                string[] kickArgs = args.Split(' ', 2);
                if (kickArgs.Length > 0 && !string.IsNullOrEmpty(kickArgs[0]))
                {
                    string userToKick = kickArgs[0];
                    string? reason = kickArgs.Length > 1 ? kickArgs[1] : null;

                    await client.KickAsync(currentChannel.ChannelId, userToKick, reason);
                }
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /KICK <nick> [reason]");
                break;

            case "KILL":
                string[] killArgs = args.Split(" ", 2);
                if (killArgs.Length == 2)
                    await client.KillAsync(killArgs[0], killArgs[1]);
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /KILL <nick> <reason>");
                break;

            case "CLEAR":
            case "CLS":
                currentChannel.ClearMessages();
                break;

            case "NICK":
                if (!string.IsNullOrEmpty(args))
                    await client.SendRawAsync($"NICK {args}");
                else
                    currentChannel.AddMessage("SYSTEM", "Usage: /NICK <nickname>");
                break;

            case "RAW":
                if (!string.IsNullOrEmpty(args))
                    await client.SendRawAsync(args);
                break;

            case "HELP":
            case "COMMANDS":
                currentChannel.AddMessage("SYSTEM",
                    "--- Available slash commands ---\n" +
                    "JOIN    PART     ME       AWAY\n" +
                    "MSG     NOTICE   TOPIC    LIST\n" +
                    "WHOIS   MOTD     TIME     NAMES\n" +
                    "ADMIN   OPER     KICK     KILL\n" +
                    "NICK    RAW      HELP     COMMANDS"
                    );
                break;

            default:
                currentChannel.AddMessage("SYSTEM", "Command Error: Invalid command!");
                break;
        }
    }
}
