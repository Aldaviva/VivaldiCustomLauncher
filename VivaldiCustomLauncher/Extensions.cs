namespace VivaldiCustomLauncher;

public static class Extensions {

    extension(Path) {

        public static bool IsFileSystemPath(string location) =>
            !Uri.TryCreate(location, UriKind.Absolute, out Uri validUri) || validUri.IsFile;

    }

}