namespace BraidKit.Core.Helpers;

public static class ConsoleHelper
{
    public static void WriteWarning(string message)
    {
        using var _ = new TempConsoleColor(ConsoleColor.Yellow);
        Console.WriteLine(message);
    }

    public static void WriteError(string message)
    {
        using var _ = new TempConsoleColor(ConsoleColor.Red);
        Console.Error.WriteLine(message);
    }
}

public class TempConsoleColor : IDisposable
{
    private ConsoleColor _initialColor = Console.ForegroundColor;
    public TempConsoleColor(ConsoleColor color) => Console.ForegroundColor = color;
    public void Dispose() => Console.ForegroundColor = _initialColor;
}

/// <summary>Action that triggers when Ctrl+C is pressed in a console application</summary>
public class ConsoleCancelAction : IDisposable
{
    private ConsoleCancelEventHandler _handler;
    public ConsoleCancelAction(Action action)
    {
        _handler = new((_, e) =>
        {
            e.Cancel = true; // Cancel the cancel event
            action();
        });
        Console.CancelKeyPress += _handler;
    }
    public void Dispose() => Console.CancelKeyPress -= _handler;
}
