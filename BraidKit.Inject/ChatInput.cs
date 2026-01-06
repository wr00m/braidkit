using BraidKit.Core.Helpers;
using BraidKit.Core.Network;
using System.Diagnostics.CodeAnalysis;

namespace BraidKit.Inject;

internal class ChatInput
{
    private readonly KeyboardState _keyboardState = new();
    public string? Message { get; private set; }
    [MemberNotNullWhen(true, nameof(Message))] public bool IsActive => Message is not null;

    /// <returns>True if a new message was completed</returns>
    public bool Update([NotNullWhen(true)] out string? completedMessage)
    {
        _keyboardState.UpdateState();

        // Toggle typing on/off if enter key was pressed
        if (_keyboardState.WasKeyJustPressed(VirtualKey.Enter))
        {
            if (IsActive)
            {
                completedMessage = !string.IsNullOrWhiteSpace(Message) ? Message : null;
                Message = null;
                return completedMessage is not null;
            }

            Message = ""; // Activate typing
            completedMessage = null;
            return false;
        }

        // Early exit if typing isn't active
        if (!IsActive)
        {
            completedMessage = null;
            return false;
        }

        // Erase last char if backspace key was pressed
        if (_keyboardState.WasKeyJustPressed(VirtualKey.Back))
        {
            Message = Message[..Math.Max(Message.Length - 1, 0)];
            completedMessage = null;
            return false;
        }

        if (_keyboardState.TryGetTypedChars(out var chars))
            Message = (Message + chars).Truncate(PacketConstants.ChatMessageMaxLength);

        completedMessage = null;
        return false;
    }
}
