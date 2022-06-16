using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModAPI.Utils;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class Task : Popup
    {   
        public Task(ProgressHandler progressHandler)
        {
            ProgressHandler = progressHandler;
            ProgressHandler.OnFinish += () =>
            {
                if (OnClose != null)
                    OnClose();
            };
            ProgressHandler.OnError += (text) =>
            {
                IsCloseVisible = true;
            };
        }

        private ProgressHandler _ProgressHandler;
        public ProgressHandler ProgressHandler { get => _ProgressHandler; set => this.RaiseAndSetIfChanged<Task, ProgressHandler>(ref _ProgressHandler, value, "ProgressHandler"); }
        private string _Name;
        public string Name { get => _Name; set => this.RaiseAndSetIfChanged<Task, string>(ref _Name, value, "Name"); }
        private bool _IsCloseVisible;
        public bool IsCloseVisible { get => _IsCloseVisible; set => this.RaiseAndSetIfChanged<Task, bool>(ref _IsCloseVisible, value, "IsCloseVisible"); }
        private bool _IsCancelVisible;
        public bool IsCancelVisible { get => _IsCancelVisible; set => this.RaiseAndSetIfChanged<Task, bool>(ref _IsCancelVisible, value, "IsCancelVisible"); }


    }
}
