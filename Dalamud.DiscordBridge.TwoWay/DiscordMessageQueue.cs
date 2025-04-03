using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Dalamud.DiscordBridge.Model;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace Dalamud.DiscordBridge
{
    public class DiscordMessageQueue
    {
        static readonly IPluginLog Logger = Service.Logger;
        private volatile bool runQueue = true;

        private readonly DiscordBridgePlugin Plugin;
        private readonly Thread runnerThread;

        private readonly ConcurrentQueue<QueuedXivEvent> eventQueue = new();

        public DiscordMessageQueue(DiscordBridgePlugin plugin)
        {
            this.Plugin = plugin;
            this.runnerThread = new Thread(RunMessageQueue);
        }

        public void Start()
        {
            this.runQueue = true;
            this.runnerThread.Start();
        }

        public void Stop()
        {
            this.runQueue = false;

            if(this.runnerThread.IsAlive)
                this.runnerThread.Join();
        }

        public void Enqueue(QueuedXivEvent @event) => this.eventQueue.Enqueue(@event);

        private async void RunMessageQueue()
        {
            while (this.runQueue)
            {
                if (this.eventQueue.TryDequeue(out var resultEvent))
                {
                    try
                    {

                        if (resultEvent is QueuedRetainerItemSaleEvent retainerSaleEvent)
                        {
                            try
                            {

                                //foreach (var regex in retainerSaleRegexes[this.plugin.Interface.ClientState.ClientLanguage])
                                {
                                    //var matchInfo = regex.Match(retainerSaleEvent.Message.TextValue);


                                    var avatarUrl = Constant.LogoLink;
                                    var itemSheet = Service.Data.GetExcelSheet<Item>();
                                    Item itemRow;

                                    if (retainerSaleEvent.Message.Payloads.First(x => x.Type == PayloadType.Item) is not ItemPayload itemLink)
                                    {
                                        Logger.Error("itemLink was null. Msg: {0}", BitConverter.ToString(retainerSaleEvent.Message.Encode()));
                                        break;
                                    }
                                    else
                                    {
                                        itemRow = itemSheet.GetRow(itemLink.Item.RowId);
                                        
                                        
                                        // XIVAPI wants these padded with 0s in the front if under 6 digits
                                        // at least if Titanium Ore testing is to be believed. 
                                        var iconFolder = $"{itemRow.Icon / 1000 * 1000}".PadLeft(6,'0');
                                        var iconFile = $"{itemRow.Icon}".PadLeft(6, '0');

                                        // avatarUrl = $"https://xivapi.com" + $"/i/{iconFolder}/{iconFile}.png";
                                        avatarUrl = $"https://beta.xivapi.com/api/1/asset?path=ui%2Ficon%2F" + $"{iconFolder}%2F{iconFile}_hr1.tex&format=png";
                                        /* 
                                        // we don't need this anymore because the above should work
                                        // but it doesn't hurt to have it commented out as a fallback for the future
                                        try
                                        {
                                            ItemResult res = XivApiClient.GetItem(itemLink.Item.RowId).GetAwaiter().GetResult();
                                            avatarUrl = $"https://xivapi.com{res.Icon}";
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error(ex, "Cannot fetch XIVAPI item search.");
                                        }
                                        */
                                    }

                                    //var valueInfo = matchInfo.Groups["value"];
                                    // not sure if using a culture here would work correctly, so just strip symbols instead
                                    //if (!valueInfo.Success || !int.TryParse(valueInfo.Value.Replace(",", "").Replace(".", ""), out var itemValue))
                                    //    continue;

                                    //SendItemSaleEvent(uint itemId, int amount, bool isHq, string message, XivChatType chatType)

                                    await this.Plugin.Discord.SendItemSaleEvent(itemRow.Name, avatarUrl, itemLink.Item.RowId, retainerSaleEvent.Message.TextValue, retainerSaleEvent.ChatType);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, "Could not send discord message.");
                            }
                        }

                        if (resultEvent is QueuedChatEvent chatEvent)
                        {
                            var senderName = (chatEvent.ChatType == XivChatType.TellOutgoing || chatEvent.ChatType == XivChatType.Echo)
                                ? Plugin.cachedLocalPlayer.Name
                                : chatEvent.Sender.ToString();
                            var senderWorld = string.Empty;


                            // for debugging. Make sure to comment this out for releases.
                            /*
                            Logger.Debug($"Type: {chatEvent.ChatType} Sender: {chatEvent.Sender.TextValue} "
                                + $"Message: {chatEvent.Message.TextValue}");
                            */

                            try
                            {
                                if (Plugin.cachedLocalPlayer != null)
                                {
                                    if (chatEvent.Sender.Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) is not PlayerPayload playerLink)
                                    {
                                        // chat messages from the local player do not include a player link, and are just the raw name
                                        // but we should still track other instances to know if this is ever an issue otherwise

                                        // Special case 2 - When the local player talks in party/alliance, the name comes through as raw text,
                                        // but prefixed by their position number in the party (which for local player may always be 1)
                                        if (chatEvent.Sender.TextValue.EndsWith(Plugin.cachedLocalPlayer.Name.TextValue))
                                        {
                                            senderName = Plugin.cachedLocalPlayer.Name;
                                        }
                                        else
                                        {
                                            // Franz is really tired of getting playerlink is null when there shouldn't be a player link for certain things
                                            switch (chatEvent.ChatType)
                                            {
                                                case XivChatType.Debug:
                                                    break;
                                                case XivChatType.Urgent:
                                                    break;
                                                case XivChatType.Notice:
                                                    break;
                                                case XivChatType.TellOutgoing:
                                                    senderName = Plugin.cachedLocalPlayer.Name;
                                                    // senderWorld = this.plugin.Interface.ClientState.LocalPlayer.HomeWorld.GameData.Name;
                                                    break;
                                                case XivChatType.StandardEmote:
                                                    playerLink = chatEvent.Message.Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) as PlayerPayload;
                                                    senderName = playerLink.PlayerName;
                                                    senderWorld = playerLink.World.Value.Name.ExtractText();
                                                    // we need to get the world here because cross-world people will be assumed local player's otherwise.
                                                    /*
                                                    senderWorld = chatEvent.Message.TextValue.TrimStart(senderName.ToCharArray()).Split(' ')[0];
                                                    if (senderWorld.EndsWith("'s")) // fuck having to do this
                                                        senderWorld = senderWorld.Substring(0, senderWorld.Length - 2);
                                                    */
                                                    break;
                                                case XivChatType.Echo:
                                                    senderName = Plugin.cachedLocalPlayer.Name;
                                                    // senderWorld = this.plugin.Interface.ClientState.LocalPlayer.HomeWorld.GameData.Name;
                                                    break;
                                                case (XivChatType)61: // NPC Talk
                                                    senderName = chatEvent.Sender.TextValue;
                                                    senderWorld = "NPC";
                                                    break;
                                                case (XivChatType)68: // NPC Announcement
                                                    senderName = chatEvent.Sender.TextValue;
                                                    senderWorld = "NPC";
                                                    break;
                                                default:
                                                    if ((int)chatEvent.ChatType >= 41 && (int)chatEvent.ChatType <= 55) //ignore a bunch of non-chat messages
                                                        break;
                                                    if ((int)chatEvent.ChatType >= 57 && (int)chatEvent.ChatType <= 70) //ignore a bunch of non-chat messages
                                                        break;
                                                    if ((int)chatEvent.ChatType >= 72 && (int)chatEvent.ChatType <= 100) // ignore a bunch of non-chat messages
                                                        break;
                                                    if ((int)chatEvent.ChatType > 107) // don't handle anything past CWLS8 for now
                                                        break;
                                                    Logger.Error($"playerLink was null.\nChatType: {chatEvent.ChatType} ({(int)chatEvent.ChatType}) Sender: {chatEvent.Sender.TextValue} Message: {chatEvent.Message.TextValue}");
                                                    senderName = chatEvent.Sender.TextValue;
                                                    break;
                                            }



                                        }

                                        // only if we still need one
                                        if (senderWorld.Equals(string.Empty))
                                            senderWorld = Plugin.cachedLocalPlayer.HomeWorld.Value.Name.ExtractText();



                                        // Logger.Information($"FRANZDEBUGGINGNULL Playerlink is null: {senderName}＠{senderWorld}");
                                    }
                                    else
                                    {
                                        senderName = chatEvent.ChatType == XivChatType.TellOutgoing
                                            ? Plugin.cachedLocalPlayer.Name
                                            : playerLink.PlayerName;
                                        senderWorld = chatEvent.ChatType == XivChatType.TellOutgoing
                                            ? Plugin.cachedLocalPlayer.HomeWorld.Value.Name.ExtractText()
                                            : playerLink.World.Value.Name.ExtractText();
                                        // Logger.Information($"FRANZDEBUGGING Playerlink was not null: {senderName}＠{senderWorld}");
                                    }
                                }
                                else
                                {
                                    // only do this one if it's debug
                                    /*
                                    Logger.Debug($"Plugin interface LocalPlayer was null.\n"
                                        + $"ChatType: {chatEvent.ChatType} ({(int)chatEvent.ChatType}) Sender: {chatEvent.Sender.TextValue} Message: {chatEvent.Message.TextValue}");
                                    */
                                    senderName = string.Empty;
                                    senderWorld = string.Empty;
                                }
                            }
                            catch(Exception ex)
                            {
                                Logger.Error(ex, "Could not deduce player name.");
                            }

                            string messagetext = "";
                            chatEvent.Message.Payloads.ForEach(x =>
                            {
                                if (x.Type == PayloadType.EmphasisItalic)
                                {
                                    messagetext += "_";
                                }
                                else if (x.Type == PayloadType.UIGlow)
                                {
                                    messagetext += "**";
                                }
                                else if (x is ITextProvider provider)
                                {
                                    messagetext += provider.Text;
                                }
                            });

                            try
                            {
                                await this.Plugin.Discord.SendChatEvent(messagetext, senderName.TextValue, senderWorld, chatEvent.ChatType, chatEvent.AvatarUrl);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, "Could not send discord message.");
                            }
                        }

                        if (resultEvent is QueuedContentFinderEvent cfEvent)
                            try
                            {
                                await this.Plugin.Discord.SendContentFinderEvent(cfEvent);
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, "Could not send discord message.");
                            }

                        
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Could not process event.");
                    }
                }

                Thread.Yield();
            }
        }
    }
}
