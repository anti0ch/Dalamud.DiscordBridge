using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace Dalamud.DiscordBridge.Helper;

/// A class containing chat functionality
public class MessageRelay
{
    private readonly ISigScanner scanner;

    internal MessageRelay(ISigScanner scanner)
    {
        this.scanner = scanner;
    }

    /// <summary>
    /// Send a given message to the chat box. <b>This can send chat to the server.</b>
    /// </summary>
    /// <b>This method is unsafe.</b> This method does no checking on your input and
    /// may send content to the server that the normal client could not. You must
    /// verify what you're sending and handle content and length to properly use this.
    /// <param name="message">Message to send</param>
    /// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
    public unsafe void SendMessageUnsafe(byte[] message)
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null)
        {
            throw new InvalidOperationException("The UiModule is currently unavailable");
        }

        using var payload = new ChatPayload(message);
        var mem1 = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, mem1, false);

        uiModule->ProcessChatBoxEntry((Utf8String*)mem1);
        Marshal.FreeHGlobal(mem1);
    }

    /// Send a given message to the chat box. <b>This can send chat to the server.</b>
    /// This method is slightly less unsafe than <see cref="SendMessageUnsafe"/>. It
    /// will throw exceptions for certain inputs that the client can't normally send,
    /// but it is still possible to make mistakes. Use with caution.
    /// <param name="message">message to send</param>
    /// <exception cref="ArgumentException">If <paramref name="message"/> is empty, longer than 500 bytes in UTF-8, or contains invalid characters.</exception>
    /// <exception cref="InvalidOperationException">If the UiModule is currently unavailable</exception>
    public unsafe void SendMessage(string message)
    {
        var utf8 = Utf8String.FromString(message);

        try
        {
            if (utf8->Length == 0)
            {
                throw new ArgumentException("message is empty", nameof(message));
            }

            if (utf8->Length > 500)
            {
                throw new ArgumentException("message is longer than 500 bytes", nameof(message));
            }

            var uiModule = UIModule.Instance();
            if (uiModule == null)
            {
                throw new InvalidOperationException("The UiModule is currently unavailable");
            }

            // Basic sanitization that allows command characters
            var oldLength = utf8->Length;
            utf8->SanitizeString(AllowedEntities.UppercaseLetters | AllowedEntities.LowercaseLetters | AllowedEntities.Numbers | 
                               AllowedEntities.SpecialCharacters | AllowedEntities.CharacterList | AllowedEntities.OtherCharacters | 
                               AllowedEntities.Payloads | AllowedEntities.Unknown9 | AllowedEntities.CJK);

            if (utf8->Length != oldLength)
            {
                throw new ArgumentException("message contained invalid characters", nameof(message));
            }

            uiModule->ProcessChatBoxEntry(utf8);
        }
        finally
        {
            if (utf8 != null)
            {
                utf8->Dtor(true);
            }
        }
    }

    /// Sanitises a string by removing any invalid input.
    /// The result of this method is safe to use with <see cref="SendMessage"/>, provided that it is not empty or too long.
    /// <param name="text">text to sanitise</param>
    /// <returns>sanitised text</returns>
    public unsafe string SanitiseText(string text)
    {
        var uText = Utf8String.FromString(text);

        uText->SanitizeString(AllowedEntities.UppercaseLetters | AllowedEntities.LowercaseLetters | AllowedEntities.Numbers | 
                            AllowedEntities.SpecialCharacters | AllowedEntities.CJK, (Utf8String*)IntPtr.Zero);
        var sanitised = uText->ToString();

        uText->Dtor();
        IMemorySpace.Free(uText);

        return sanitised;
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly IntPtr textPtr;

        [FieldOffset(16)]
        private readonly ulong textLen;

        [FieldOffset(8)]
        private readonly ulong unk1;

        [FieldOffset(24)]
        private readonly ulong unk2;

        internal ChatPayload(byte[] stringBytes)
        {
            textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);
            Marshal.Copy(stringBytes, 0, this.textPtr, stringBytes.Length);
            Marshal.WriteByte(this.textPtr + stringBytes.Length, 0);

            textLen = (ulong)(stringBytes.Length + 1);

            unk1 = 64;
            unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }
}