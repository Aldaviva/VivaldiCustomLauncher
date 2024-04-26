#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace VivaldiCustomLauncher.Tweaks;

public class TweakException(string message, string tweakTypeName, [CallerMemberName] string tweakMethodName = ""): Exception(message) {

    public string tweakTypeName { get; } = tweakTypeName;
    public string tweakMethodName { get; } = tweakMethodName;

    public override string Message => $"{bareMessage} in {tweakTypeName}.{tweakMethodName}()";

    public string bareMessage => base.Message;

}