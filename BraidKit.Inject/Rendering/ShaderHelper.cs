using BraidKit.Core.Helpers;
using System.Reflection;
using Vortice.D3DCompiler;

namespace BraidKit.Inject.Rendering;

internal static class ShaderHelper
{
    public static ReadOnlySpan<byte> CompileShader(string embeddedFilename, string entryPoint, string profile)
    {
        var shaderSource = Assembly.GetExecutingAssembly().ReadEmbeddedTextFile(embeddedFilename);
        var result = Compiler.Compile(shaderSource, entryPoint, embeddedFilename, profile, out var compiledBlob, out var errorBlob);
        if (result.Failure)
            Logger.Log($"Shader compilation failed: {errorBlob?.AsString() ?? "Unknown error"}");
        return compiledBlob.AsSpan();
    }
}
