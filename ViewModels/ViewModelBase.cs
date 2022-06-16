using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        private bool _IsActive;
        public bool IsActive { get => _IsActive; set => this.RaiseAndSetIfChanged<ViewModelBase, bool>(ref _IsActive, value, "IsActive"); }
    }
}
