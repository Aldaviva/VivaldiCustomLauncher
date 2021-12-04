using System;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    internal class TweakException: Exception {

        public string tweakTypeName { get; }
        public string tweakMethodName { get; }

        public TweakException(string message, string tweakTypeName, string tweakMethodName): base(message) {
            this.tweakTypeName   = tweakTypeName;
            this.tweakMethodName = tweakMethodName;
        }

    }

}