using System;
using System.Collections.Generic;
using System.Text;

namespace BaseModLib
{
    /**
     * Decorates your method to be run when a certain level is loaded.
     * 
     * Treats the method as if it is static. So use static or don't use this without null check.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExecuteOnLevelLoad : System.Attribute
    {
        private int LevelNum;
        private string LevelName;

        public ExecuteOnLevelLoad(int levelNum = -1)
        {
            LevelNum = levelNum;
        }

        public ExecuteOnLevelLoad(string levelName)
        {
            LevelName = levelName;
        }
    }
}
