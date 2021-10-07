using System;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    internal class TweakException: Exception {

        public string tweakTypeName { get; }
        public string tweakMethodName { get; }

        public TweakException(string tweakTypeName, string tweakMethodName, string message): base(message) {
            this.tweakTypeName   = tweakTypeName;
            this.tweakMethodName = tweakMethodName;
        }

    }

}