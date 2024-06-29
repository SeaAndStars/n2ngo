using N2NGO_Core;
using System.Diagnostics;
using System.IO.Compression;

namespace N2NGOClientUpdate
{
    internal class Program
    {
        readonly static string N2NGOClientUpdateMutexKey = "N2NGOClientUpdateMtx";
        readonly static string TempPath = Path.Combine(UserDef.N2NGO_N2NGO_Client_Update_AppData_Path, "Temp");
        readonly static string DownloadFile = "windowsdesktop-runtime-6.0.30-win-x86.exe";
        readonly static string DownloadUrl = "https://download.visualstudio.microsoft.com/download/pr/94bd5cf9-0c22-4790-89c6-d1ce4b4fe952/2a01badbae5ec0c3e199f3c2a7ae764f/windowsdesktop-runtime-6.0.30-win-x86.exe";
        static string DownloadPath { get => Path.Combine(TempPath, DownloadFile); }

        static void DebugPause()
        {
#if DEBUG
            Console.ReadKey(false);
#endif
        }

        static void Main(string[] args)
        {
            Console.Title = "N2N GO Updater";
            Info();

            if (args.Length < 1)
            {
                goto err_arg_exit;
            }
            if (args[0] == "-h" || args[0] == "--help")
            {
                Help();
                DebugPause();
                Environment.Exit(0);
            }

            if (args.Length < 2)
                goto err_arg_exit;

            if (!int.TryParse(args[0], out int step))
            {
                Console.WriteLine($"Invalid argument 0: '{args[0]}' for {nameof(step)}");
                goto err_arg_exit;
            }

            if (!int.TryParse(args[1], out int pid))
            {
                Console.WriteLine($"Invalid argument 1: '{args[1]}' for {nameof(pid)}");
                goto err_arg_exit;
            }

            try
            {

                switch (step)
                {
                    default:
                        {
                            Console.WriteLine($"Invalid argument 0: '{args[0]}' for {nameof(step)}");
                            goto err_arg_exit;
                        }

                    case 0:
                        {
                            try { Process.GetProcessById(pid).WaitForExit(); } catch { }

                            Mutex mtx_this = new(true, N2NGOClientUpdateMutexKey, out var mtx_get);
                            if (mtx_get)
                            {
                                var srcPath = Environment.ProcessPath ?? throw new NullReferenceException($"{nameof(Environment.ProcessPath)} null");
                                var destCopyPath = Path.Combine(TempPath, Path.GetFileName(Environment.ProcessPath));
                                var destCopyDirectoryPath = Path.GetDirectoryName(destCopyPath);

                                if (destCopyDirectoryPath is not null&&!Directory.Exists(destCopyDirectoryPath))
                                    Directory.CreateDirectory(destCopyDirectoryPath);

                                {
                                    FileStream fileStream = new(srcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                    var fileInBytes = new byte[fileStream.Length];
                                    fileStream.Read(fileInBytes);
                                    fileStream.Close();
                                    fileStream.Dispose();

                                    //File.WriteAllBytes(destCopyPath, fileInBytes);
                                }
                                File.Copy(srcPath, destCopyPath, true);

                                Process.Start(destCopyPath, new string[3] { 1.ToString(), Environment.ProcessId.ToString(), Path.GetDirectoryName(Environment.ProcessPath) ?? throw new NullReferenceException($"{nameof(Path.GetDirectoryName)} null") });

                                mtx_this.ReleaseMutex();
                            }
                            else
                                goto err_mtx_exit;

                            DebugPause();
                            Environment.Exit(0);
                            break;
                        }

                    case 1:
                        {
                            if (args.Length < 3)
                            {
                                goto err_arg_exit;
                            }

                            Mutex mtx_this = new(true, N2NGOClientUpdateMutexKey, out var mtx_get);
                            if (mtx_get)
                            {
                                try { Process.GetProcessById(pid).WaitForExit(); } catch { }

                                if (File.Exists(UserDef.UpdatePackageFilePath))
                                {
                                    ZipFile.ExtractToDirectory(UserDef.UpdatePackageFilePath, args[2], true);
                                    File.Delete(UserDef.UpdatePackageFilePath);
                                }

                                try
                                {
                                    Process DotNetCheck = new()
                                    {
                                        StartInfo = new ProcessStartInfo { CreateNoWindow = true, RedirectStandardOutput = true, FileName = "dotnet", Arguments = "--list-runtimes" }
                                    };
                                    DotNetCheck.Start();
                                    DotNetCheck.WaitForExit();

                                    string output = DotNetCheck.StandardOutput.ReadToEnd();
                                    if (!output.Contains("Microsoft.NETCore.App 6") || !output.Contains("Microsoft.WindowsDesktop.App 6"))
                                        InstallDotNet_6_Core_Desktop();
                                }
                                catch { InstallDotNet_6_Core_Desktop(); }


                                mtx_this.ReleaseMutex();
                            }
                            else
                                goto err_mtx_exit;

                            N2NGO.UtilsClass.EasyConfig cfg = new(Path.Combine(N2NGO_Core.UserDef.N2NGO_N2NGO_AppData_Path, "UserData/config.ini"));
                            cfg.Set("NeedUpdate", "0");
                            cfg.SaveConfigDataToFile();

                            Process.Start(Path.Combine(args[2], "N2NGO.exe"));

                            DebugPause();
                            Environment.Exit(0);
                            break;
                        }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                DebugPause();
            }

            DebugPause();

        err_arg_exit:
            {
                var bak = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Invalid arguments.(-1)\n");
                Console.ForegroundColor = bak;
                Help();
                PressAnyKeyToContinue();
                Environment.Exit(-1);
            }
        err_mtx_exit:
            {
                var bak = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Cannot get mutex lock, is there any N2N GO Updater running?(-2)\n");
                Console.ForegroundColor = bak;
                PressAnyKeyToContinue();
                Environment.Exit(-2);
            }
        }

        static void Info()
        {
            Console.WriteLine(
                "N2N GO Client Updater\n" +
                ""
                );

            var bak = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: Please do not over rely on this program, it is not a standard installer and we will plan to replace it with another installer in the near future.\n");
            Console.ForegroundColor = bak;
        }

        static void Help()
        {
            Console.WriteLine(
                "Usage: \"N2NGO Client Update\" [OPTIONS] STEP PID [PATH]\n" +
                "\n" +
                "Arguments:\n" +
                "  STEP         Installation step (required)\n" +
                "               When STEP is 1, PATH is also required.\n" +
                "  PID          Process-ID to wait for exit before installation begins (required)\n" +
                "  PATH         Installation path (optional, except when STEP is 1)\n" +
                "\n" +
                "Options:\n" +
                "  -h, --help   Show this help message and exit\n" +
                "\n" +
                ""
                );
        }

        static void PressAnyKeyToContinue()
        {
            Console.WriteLine("Press Any Key...");
            Console.ReadKey(false);
        }

        static async void InstallDotNet_6_Core_Desktop()
        {
            Directory.CreateDirectory(TempPath);

            await DownloadFileAsync(DownloadUrl, DownloadPath);

            Process.Start(DownloadPath, "/quiet").WaitForExit();
        }

        public static async Task DownloadFileAsync(string url, string path)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"{e.Message}");
            }

            using FileStream fileStream = File.Create(path);
            await response.Content.CopyToAsync(fileStream);
        }
    }
}