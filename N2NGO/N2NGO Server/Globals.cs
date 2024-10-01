namespace N2NGOServer;

internal static class Globals
{
    public static readonly N2NGOCore.EasyConfig Config = new("Storage/Data/config.ini");

    public static Version N2NGOClientLatestVersion { get => Version.Parse(File.ReadAllText("Storage/Resources/ClientLatestVersion.txt")); }
}
