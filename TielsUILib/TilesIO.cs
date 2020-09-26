using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TielsUILib
{
    class TilesIO
    {
        public static void CreateNewTile()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                try
                {
                    //File.AppendAllText("R:\\debug.md", "* " + p.MainModule.FileName + "\n");
                    if (p.MainModule.FileName.Contains("Tiels.exe"))
                    {
                        // Pipes
                    }
                }
                catch (Exception igonre)
                {
                }
            }
        }
    }
}
