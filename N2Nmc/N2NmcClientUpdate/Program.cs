using N2Nmc_Protocol;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace N2NmcClientUpdate
{
    internal class Program
    {
        readonly static string N2NmcUpdatePackPath = UserDef.UpdatePackageFileName;
        readonly static string tempPath = "temp";
        readonly static string downloadFile = "windowsdesktop-runtime-6.0.30-win-x86.exe";
        readonly static string downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/94bd5cf9-0c22-4790-89c6-d1ce4b4fe952/2a01badbae5ec0c3e199f3c2a7ae764f/windowsdesktop-runtime-6.0.30-win-x86.exe";
        static string downloadPath { get => Path.Combine(tempPath, downloadFile); }

        static void Main(string[] args)
        {
            Console.Title = "N2Nmc升级程序";

            if (args.Length < 1)
                goto err_arg_exit;

            int pid = 0;
            bool bp_pid = int.TryParse(args[0], out pid);

            if (pid == 0 || !bp_pid)
                goto err_arg_exit;

            bool mtx_get = false;
            Mutex mtx_this = new Mutex(true, "N2NmcClientUpdateMtx", out mtx_get);

            if (mtx_get)
            {
                try { Process.GetProcessById(pid).WaitForExit(); } catch { }

                if (File.Exists(N2NmcUpdatePackPath))
                {
                    ZipFile.ExtractToDirectory(N2NmcUpdatePackPath, ".", true);
                    File.Delete(N2NmcUpdatePackPath);
                }

                try
                {
                    Process DotNetCheck = new();
                    DotNetCheck.StartInfo = new ProcessStartInfo { CreateNoWindow = true, RedirectStandardOutput = true, FileName = "dotnet", Arguments = "--list-runtimes" };
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

            N2Nmc.UtilsClass.EasyConfig cfg = new N2Nmc.UtilsClass.EasyConfig("config.ini");
            cfg.Set("NeedUpdate", "0");
            cfg.SaveConfigDataToFile();

            Process.Start("N2Nmc.exe");

            return;

        err_arg_exit:
            Console.WriteLine("Error Arg for PID");
            Environment.Exit(-1);
        err_mtx_exit:
            Console.WriteLine("Error Mutex Get");
            Environment.Exit(-2);
        }

        static void InstallDotNet_6_Core_Desktop()
        {
            Directory.CreateDirectory(tempPath);

            DownloadFileAsync(downloadUrl, downloadPath);

            Process.Start(downloadPath, "/quiet").WaitForExit();
        }

        public static async Task DownloadFileAsync(string url, string path)
        {
            using (HttpClient client = new())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"{e.Message}");
                    }

                    using (FileStream fileStream = File.Create(path))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }
        }
    }
}