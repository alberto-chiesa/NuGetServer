namespace NuGetServer {
    public static class AvailableRoles {
        public const string Administrator = "Administrator";
        public const string Reader        = "Reader";
        public static readonly string[] AllRoles = new[] { Reader, Administrator };
    }
}