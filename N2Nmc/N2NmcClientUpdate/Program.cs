using N2Nmc_Protocol;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace N2NmcClientUpdate
{
    internal class Program
    {
        readonly static string N2NmcUpdatePackPath = UserDef.UpdatePackageFileName;
        readonly static string downloadFile = "windowsdesktop-runtime-6.0.0-rc.2.21501.6-win-x86.exe";
        readonly static string downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/9c58ffd6-cdfc-4cae-a163-247bb22c4e24/93601cca92711d2d03fdb7f7dab88bc2/windowsdesktop-runtime-6.0.0-rc.2.21501.6-win-x86.exe";

        static void InstallDotNet_6_Core_Desktop()
        {
            string path = "temp/"+downloadFile;
            Directory.CreateDirectory("temp");

            WebClient webClient = new WebClient();
            webClient.DownloadFile(downloadUrl, path);

            Process.Start(path,"/quiet").WaitForExit();
        }

        static void Main(string[] args)
        {
            Console.Title = "N2Nmc升级程序";

            if (args.Length < 1)
                goto err_arg_exit;

            int pid = 0;
            bool bp_pid = int.TryParse(args[0], out pid);

            if (pid==0|| !bp_pid)
                goto err_arg_exit;

            bool mtx_get = false;
            Mutex mtx_this = new Mutex(true,"N2NmcClientUpdateMtx",out mtx_get);

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
                    Process DotNetCheck = new Process();
                    DotNetCheck.StartInfo = new ProcessStartInfo { CreateNoWindow = true,RedirectStandardOutput =true , FileName = "dotnet", Arguments = "--list-runtimes" };
                    DotNetCheck.Start();
                    DotNetCheck.WaitForExit();

                    string output =DotNetCheck.StandardOutput.ReadToEnd();
                    if (!output.Contains("Microsoft.NETCore.App 6")||!output.Contains("Microsoft.WindowsDesktop.App 6"))
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
    }
}