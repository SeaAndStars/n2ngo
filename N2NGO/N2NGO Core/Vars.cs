namespace N2NGOCore;

public static class Vars
{
    public static class ExtendServerOptions
    {
        public readonly static int N2NGO_File_Server_Port = 7478;
    }

    public readonly static string N2NGOUpdateInstallerFileName = "N2NGOUpdatePack.pak";

    public static string N2NGO_Base_AppData_Path => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "N2NGO");
    public static string N2NGO_N2NGO_AppData_Path => Path.Combine(N2NGO_Base_AppData_Path, "N2NGO");
    public static string N2NGO_N2NGO_Client_Update_AppData_Path => Path.Combine(N2NGO_Base_AppData_Path, "N2NGO Client Update");
    public static string N2NGO_N2NGO_Server_AppData_Path => Path.Combine(N2NGO_Base_AppData_Path, "N2NGO Server");

    public static string N2NGOUpdateInstallerFilePath => Path.Combine(N2NGO_Base_AppData_Path, N2NGOUpdateInstallerFileName);
}
