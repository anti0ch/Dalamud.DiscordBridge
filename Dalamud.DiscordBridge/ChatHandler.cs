using System;
using System.Collections.Generic;
using Dalamud.DiscordBridge.Helper;
using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace Dalamud.DiscordBridge
{
public class ChatHandler
{
   static readonly IPluginLog Logger = Service.Logger;
   private readonly MessageRelay messageRelay;
   private const int MaxMessageLength = 500;
   private const string ContinuationMarker = " >>";

   public ChatHandler(ISigScanner scanner)
   {
      messageRelay = new MessageRelay(scanner);
   }

   public (bool success, string replyMessage) HandleMessage(string channel, string message)
   {
      Logger.Debug($"[ChatHandler] Channel: {channel}, Message: {message}");
      try
      {
         if (!message.StartsWith("/"))
         {
            Logger.Warning("Message must start with /");
            return (false, "Message must start with /");
         }

         Logger.Debug($"Sending command: {message}");
         if (message.Length > MaxMessageLength)
         {
            Logger.Warning("Message too long!");
            return (false, $"Message too long! Here's the maximum you can send:\n{message.Substring(0, MaxMessageLength)}");
         }

         if (!string.IsNullOrEmpty(channel))
         {
            // Format it as a chat message with channel
            string formattedMessage = $"{channel} {message}";
            Service.Framework.RunOnFrameworkThread(() =>
               messageRelay.SendMessage(formattedMessage));
            return (true, null);
         }
         else
         {
            // If no channel specified, send the message directly
            Service.Framework.RunOnFrameworkThread(() =>
               messageRelay.SendMessage(message));
            return (true, null);
         }
      }
      catch (Exception ex)
      {
         Logger.Error($"Failed to send message: {ex.Message}");
         return (false, "Failed to send message");
      }
   }

   private List<string> SplitMessage(string message)
   {
      var messages = new List<string>();
      var remainingMessage = message;
      var partCount = 0;
      var maxPartLength = MaxMessageLength - (ContinuationMarker.Length * 2); // Account for both start and end markers

      while (remainingMessage.Length > 0)
      {
         partCount++;
         if (remainingMessage.Length <= maxPartLength)
         {
            messages.Add(remainingMessage);
            break;
         }

         // Find the last space before maxPartLength
         var splitPoint = remainingMessage.LastIndexOf(" ", maxPartLength);
         if (splitPoint == -1)
         {
            splitPoint = maxPartLength;
         }

         var part = remainingMessage.Substring(0, splitPoint);
         messages.Add(part);
         remainingMessage = remainingMessage.Substring(splitPoint).TrimStart();
      }
      
      // Add ">>" to the beginning and end of all parts except the first (no beginning) and last (no end)
      for (int i = 1; i < messages.Count; i++)
      {
         if (i < messages.Count - 1)
         {
            // Middle parts get both start and end markers
            messages[i] = ">>" + messages[i] + ContinuationMarker;
         }
         else
         {
            // Last part only gets start marker
            messages[i] = ">>" + messages[i];
         }
      }

      return messages;
   }
}
}