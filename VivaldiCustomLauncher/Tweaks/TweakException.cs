using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class TweakException: Exception {

        public string tweakTypeName { get; }
        public string tweakMethodName { get; }

        public TweakException(string message, string tweakTypeName, [CallerMemberName] string tweakMethodName = ""): base(message) {
            this.tweakTypeName   = tweakTypeName;
            this.tweakMethodName = tweakMethodName;
        }

        public override string Message => $"{bareMessage} in {tweakTypeName}.{tweakMethodName}()";

        public string bareMessage => base.Message;

    }

}