namespace BraidKit.Core.Network;

public record ChatMessage(string Sender, string Message, PlayerColor Color)
{
    public DateTime Received { get; } = DateTime.Now;
    public TimeSpan TimeSinceReceived => DateTime.Now - Received;
    public bool Stale => TimeSinceReceived > TimeSpan.FromSeconds(10);
}
