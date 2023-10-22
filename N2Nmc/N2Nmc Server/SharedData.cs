using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2Nmc_Server
{
    internal class SharedData
    {
        public static Version N2NmcClientLatestVersion { get => Version.Parse(File.ReadAllText("ClientLatestVersion.txt")); }
    }
}
