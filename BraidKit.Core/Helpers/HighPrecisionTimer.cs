using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BraidKit.Core.Helpers;

/// <summary>
/// Increases system timer resolution on Windows, allowing Thread.Sleep() and timers to be more accurate. Use with care.
/// </summary>
[SupportedOSPlatform("windows")]
public class HighPrecisionTimer : IDisposable
{
    private readonly uint _resolutionMs;
    private bool _disposed = false;

    public HighPrecisionTimer(uint resolutionMs = 1)
    {
        _resolutionMs = resolutionMs;
        _ = timeBeginPeriod(_resolutionMs);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ = timeEndPeriod(_resolutionMs);
        GC.SuppressFinalize(this);
    }

    // Finalizer, in case someone forgets to call Dispose()
    ~HighPrecisionTimer() => Dispose();

    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint uMilliseconds);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint uMilliseconds);
}