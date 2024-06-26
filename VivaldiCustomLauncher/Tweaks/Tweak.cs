﻿#nullable enable

using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public interface Tweak<OutputType, in Params> where Params: TweakParams where OutputType: class {

    /// <exception cref="TweakException"></exception>
    Task<OutputType> readAndEditFile(Params tweakParams);

    Task saveFile(OutputType fileContents, Params tweakParams);

}

public interface TweakParams {

    string filename { get; }

}

public class BaseTweakParams(string filename): TweakParams {

    public string filename { get; } = filename;

}