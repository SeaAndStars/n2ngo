using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N2Nmc_Protocol
{
    public static class UserDef
    {
        public readonly static string GlobalServer_Ip = "111.180.189.214";

        public static class ExternServerOptions
        {
            public readonly static int N2Nmc_Server_Port = 7476;

            public readonly static int N2Nmc_File_Server_Port = 7478;

            public readonly static int N2N_SuperNode_Server_Port = 7777;
            public readonly static int N2N_SuperNode_API_Server_Port = 58888;
        }

        public readonly static string UpdatePackageFileName = "N2NmcUpdatePack.pak";
    }
}
