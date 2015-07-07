using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class Engram
    {
        public string Name
        {
            get;
            set;
        }

        public int Index
        {
            get;
            set;
        }

        public int PointCode
        {
            get;
            set;
        }

        public int LevelRequirement
        {
            get;
            set;
        }

        public bool UsePrerequisite
        {
            get;
            set;
        }
    }
}
