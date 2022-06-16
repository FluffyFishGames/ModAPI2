using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class ButtonMapping : ViewModelBase
    {
        private bool _LeftShift;
        public bool LeftShift { get => _LeftShift; set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _LeftShift, value, "LeftShift"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        private bool _LeftControl;
        public bool LeftControl { get => _LeftControl; set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _LeftControl, value, "LeftControl"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        private bool _LeftAlt;
        public bool LeftAlt { 
            get => _LeftAlt; 
            set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _LeftAlt, value, "LeftAlt"); this.RaisePropertyChanged<ButtonMapping>("String"); } 
        }
        private bool _RightShift;
        public bool RightShift { get => _RightShift; set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _RightShift, value, "RightShift"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        private bool _RightControl;
        public bool RightControl { get => _RightControl; set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _RightControl, value, "RightControl"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        private bool _RightAlt;
        public bool RightAlt { get => _RightAlt; set { this.RaiseAndSetIfChanged<ButtonMapping, bool>(ref _RightAlt, value, "RightAlt"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        private UnityButton _Button;
        public UnityButton Button { get => _Button; set { this.RaiseAndSetIfChanged<ButtonMapping, UnityButton>(ref _Button, value, "Button"); this.RaisePropertyChanged<ButtonMapping>("String"); } }
        public string String 
        { 
            get 
            {
                return ToString();
            }
        }

        public override string ToString()
        {
            var ret = "";
            if (LeftControl)
                ret += "Left Ctrl + ";
            if (LeftAlt)
                ret += "Left Alt + ";
            if (LeftShift)
                ret += "Left Shift + ";
            if (RightControl)
                ret += "Right Ctrl + ";
            if (RightAlt)
                ret += "Right Alt + ";
            if (RightShift)
                ret += "Right Shift + ";
            ret += Button;
            return ret;
        }

        public JObject ToJSON()
        {
            var j = new JObject();
            j["left_control"] = LeftControl;
            j["left_alt"] = LeftAlt;
            j["left_shift"] = LeftShift;
            j["right_control"] = RightControl;
            j["right_alt"] = RightAlt;
            j["right_shift"] = RightShift;
            j["button"] = (int) Button;
            return j;
        }

        public void FromJSON(JObject obj)
        {
            LeftControl = (obj["left_control"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            LeftAlt = (obj["left_alt"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            LeftShift = (obj["left_shift"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            RightControl = (obj["right_control"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            RightAlt = (obj["right_alt"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            RightShift = (obj["right_shift"]?.ToString().ToLowerInvariant() ?? "false") == "true";
            try { Button = (UnityButton)int.Parse(obj["button"]?.ToString() ?? "0"); } catch (Exception) { }
        }

    }
}
