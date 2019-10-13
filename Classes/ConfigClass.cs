using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWinOverlay
{
    public class ConfigClass
    {
        public bool FirstRun;
        public bool Blur;
        public int Theme;
        public string Color;
        public WindowPosition[] Positions;
    }

    public class FileUpdateClass
    {
        public SoftFileData[] Data;
    }

    public class SoftFileData
    {
        public string Name;
        public int Size;
    }

    public class WindowPosition
    {
        public int X;
        public int Y;
    }
}
