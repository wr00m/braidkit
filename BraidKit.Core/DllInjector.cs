﻿using InjectDotnet;
using System.Diagnostics;

namespace BraidKit.Core;

public static class DllInjector
{
    public static bool InjectRenderer(this Process process, RenderSettings renderSettings)
    {
        return process.InjectBraidKitDllIntoGame("Render", renderSettings) != 0;
    }

    private static int InjectBraidKitDllIntoGame<TArgs>(this Process process, string method, TArgs args) where TArgs : struct
    {
        return process.Inject(
            runtimeconfig: "BraidKit.runtimeconfig.json",
            dllToInject: "BraidKit.Inject.dll",
            asssemblyQualifiedTypeName: "BraidKit.Inject.Bootstrapper, BraidKit.Inject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            method: method,
            methodArgument: args,
            waitForReturn: true)!.Value;
    }
}
