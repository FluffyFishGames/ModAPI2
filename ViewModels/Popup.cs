using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModAPI.Utils;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class Popup : ViewModelBase
    {
        public delegate void Close();
        public Close OnClose;

        public Popup()
        {
        }
    }
}
