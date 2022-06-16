using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModAPI.Utils;
using System.IO;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class CreateModProject : Popup
    {
        public Game Game;
        public CreateModProject(Game game)
        {
            Game = game;
        }

        private string _Name;
        public string Name {
            get => _Name;
            set 
            {
                if (value != _Name)
                {
                    _Name = value;
                    bool isInvalid = false;
                    foreach (var i in Path.InvalidPathChars)
                        if (_Name.Contains(i))
                            isInvalid = true;
                    if (!isInvalid && (_Name == null || _Name.Length == 0))
                        isInvalid = true;
                    var lower = value.ToLowerInvariant();
                    bool already = false;
                    foreach (var modProject in Game.ModProjects)
                    {
                        var directoryName = Path.GetFileName(modProject.Directory).ToLowerInvariant();
                        if (directoryName == lower)
                        {
                            already = true;
                            break;
                        }
                    }
                    AlreadyExists = already;
                    IsInvalid = isInvalid;
                    HasError = AlreadyExists || IsInvalid;
                    this.RaisePropertyChanged<CreateModProject>("Name");
                }
                this.RaiseAndSetIfChanged<CreateModProject, string>(ref _Name, value, "Name");
            }
        }

        private bool _HasError = true;
        public bool HasError { get => _HasError; set => this.RaiseAndSetIfChanged<CreateModProject, bool>(ref _HasError, value, "HasError"); }
        private bool _AlreadyExists;
        public bool AlreadyExists { get => _AlreadyExists; set => this.RaiseAndSetIfChanged<CreateModProject, bool>(ref _AlreadyExists, value, "AlreadyExists"); }

        private bool _IsInvalid = true;
        public bool IsInvalid { get => _IsInvalid; set => this.RaiseAndSetIfChanged<CreateModProject, bool>(ref _IsInvalid, value, "IsInvalid"); }
    }
}
