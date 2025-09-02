using System.Runtime.InteropServices;

namespace BraidKit.Core;

[StructLayout(LayoutKind.Sequential)]
public struct RenderSettings()
{
    public const float DefaultLineWidth = 2f;
    public bool RenderColliders = true;
    public float LineWidth = DefaultLineWidth;
}
