using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace ModAPI.ViewModels.ModProject
{
    public class ModButton : ViewModelBase
    {
        private ModConfiguration ProjectConfiguration;
        private Mod Mod;
        private string _ID;
        public string ID { get => _ID; set => this.RaiseAndSetIfChanged<ModButton, string>(ref _ID, value, "ID"); }
        private string _Name;
        public string Name { get => _Name; set => this.RaiseAndSetIfChanged<ModButton, string>(ref _Name, value, "Name"); }
        private string _Description;
        public string Description { get => _Description; set => this.RaiseAndSetIfChanged<ModButton, string>(ref _Description, value, "Description"); }
        private ButtonMapping _StandardMapping;
        public ButtonMapping StandardMapping { get => _StandardMapping; set => this.RaiseAndSetIfChanged<ModButton, ButtonMapping>(ref _StandardMapping, value, "StandardMapping"); }
        private ButtonMapping _Mapping;
        public ButtonMapping Mapping { get => _Mapping; set => this.RaiseAndSetIfChanged<ModButton, ButtonMapping>(ref _Mapping, value, "Mapping"); }

        public ModButton(ModConfiguration project)
        {
            ProjectConfiguration = project;
            PropertyChanged += ModProjectButton_PropertyChanged;
        }
        public ModButton(Mod mod)
        {
            Mod = mod;
            PropertyChanged += ModProjectButton_PropertyChanged;
        }

        private void ModProjectButton_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ProjectConfiguration != null)
                ProjectConfiguration.Save();
            /*if (Mod != null)
                Mod.Save();*/
        }

        public void FromJSON(JObject obj)
        {
            if (obj.ContainsKey("id"))
                ID = obj["id"].ToString();
            else throw new ArgumentException("Missing id in button configuration.");
            Name = obj["name"]?.ToString() ?? "";
            Description = obj["description"]?.ToString() ?? "";
            if (ProjectConfiguration != null)
            {
                StandardMapping = new ButtonMapping();
                if (obj.ContainsKey("standard_mapping") && obj["standard_mapping"] is JObject mapping)
                    StandardMapping.FromJSON(mapping);
            }
            if (Mod != null)
            {
                Mapping = new ButtonMapping();
                if (obj.ContainsKey("mapping") && obj["mapping"] is JObject mapping)
                    Mapping.FromJSON(mapping);
            }
        }

        public JObject ToJSON()
        {
            var bObj = new JObject();
            bObj["id"] = ID;
            if (ProjectConfiguration != null)
                bObj["standard_mapping"] = StandardMapping.ToJSON();
            if (Mod != null)
                bObj["mapping"] = Mapping.ToJSON();
            bObj["name"] = Name;
            bObj["description"] = Description;
            return bObj;
        }
    }
}
