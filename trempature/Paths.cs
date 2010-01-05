using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace trempature
{
    public static class Paths
    {
        public static string UserAppDataDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "trempature");
            }
        }

        public static void EnsureUserAppDataDir()
        {
            if (!Directory.Exists(UserAppDataDir))
            {
                Directory.CreateDirectory(UserAppDataDir);
            }
        }


    }
}
