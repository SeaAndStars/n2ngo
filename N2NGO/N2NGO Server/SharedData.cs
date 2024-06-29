using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2NGO_Server
{
    internal class SharedData
    {
        public static Version N2NGOClientLatestVersion { get => Version.Parse(File.ReadAllText("ClientLatestVersion.txt")); }
    }
}
