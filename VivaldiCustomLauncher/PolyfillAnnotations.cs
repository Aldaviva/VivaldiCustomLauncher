// Sadly, this conflicts with any other package that declares this class, such as System.Text.Json for net462. This causes build failures when using ILRepack, and requires you to internalize types to allow ILRepack to deduplicate these classes.
// If ILRepack complains about this class being a duplicate, see https://github.com/Aldaviva/Fail2Ban4Win/blob/91709d6a666f5f8977cb39c324bc5453ee5f3eec/Fail2Ban4Win/ILRepack.targets#L13

// ReSharper disable All

using System.ComponentModel;
#pragma warning disable IDE0005
using System.Diagnostics.CodeAnalysis;

#pragma warning restore IDE0005

namespace System.Runtime.CompilerServices {

    /// <summary>
    /// <para>Needed to make .NET Standard 2.0 stop breaking the build when a record type with auto-initialized property parameters is defined.</para>
    /// <para>From <see href="https://stackoverflow.com/a/62656145/979493"/></para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class IsExternalInit;

    // https://stackoverflow.com/a/74447498/979493
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class RequiredMemberAttribute: Attribute;

    // https://stackoverflow.com/a/74447498/979493
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal sealed class CompilerFeatureRequiredAttribute(string featureName): Attribute {

        public const string RefStructs      = nameof(RefStructs);
        public const string RequiredMembers = nameof(RequiredMembers);

        public string FeatureName { get; } = featureName;
        public bool IsOptional { get; init; }

    }

}

namespace System.Diagnostics.CodeAnalysis {

    [AttributeUsage(AttributeTargets.Constructor)]
    [ExcludeFromCodeCoverage]
    internal sealed class SetsRequiredMembersAttribute: Attribute;

}