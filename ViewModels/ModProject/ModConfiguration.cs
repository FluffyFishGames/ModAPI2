using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReactiveUI;
using Newtonsoft.Json.Linq;
using Mono.Cecil;

namespace ModAPI.ViewModels.ModProject
{
    public class ModConfiguration : ViewModelBase
    {
        public ModVersion _Version;
        public ModVersion Version
        {
            get => _Version;
            set
            {
                if (value != _Version)
                {
                    if (_Version != null)
                        _Version.PropertyChanged -= VersionPropertyChanged;
                    if (value != null)
                        value.PropertyChanged += VersionPropertyChanged;
                    _Version = value;
                    this.RaisePropertyChanged<ModConfiguration>("Version");
                }                
            }
        }

        private void VersionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Save();
        }

        private string _Name;
        public string Name { get => _Name; set => this.RaiseAndSetIfChanged<ModConfiguration, string>(ref _Name, value, "Name"); }

        private ModProject _Project;
        public ModProject Project { get => _Project; set => this.RaiseAndSetIfChanged<ModConfiguration, ModProject>(ref _Project, value, "Project"); }

        private Mod _Mod;
        public Mod Mod { get => _Mod; set => this.RaiseAndSetIfChanged<ModConfiguration, Mod>(ref _Mod, value, "Mod"); }

        private ObservableCollection<ModButton> _Buttons = new ObservableCollection<ModButton>();
        public ObservableCollection<ModButton> Buttons { get => _Buttons; set => this.RaiseAndSetIfChanged<ModConfiguration, ObservableCollection<ModButton>>(ref _Buttons, value, "Buttons"); }

        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => this.RaiseAndSetIfChanged<ModConfiguration, bool>(ref _IsActivated, value, "IsActivated"); }

        public HashSet<string> MethodReplaces = new();
        public HashSet<string> MethodHookBefore = new();
        public HashSet<string> MethodHookAfter = new();
        public HashSet<string> MethodChain = new();

        public ModConfiguration(ModProject project, bool load = true)
        {
            Project = project;
            if (load)
                Load();
            Buttons.CollectionChanged += ButtonsChanged;
            this.PropertyChanged += OwnPropertyChanged;
        }

        public ModConfiguration(Mod mod, bool load = true)
        {
            Mod = mod;
            if (load)
                Load();
            Buttons.CollectionChanged += ButtonsChanged;
            this.PropertyChanged += OwnPropertyChanged;
        }

        private void OwnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Save();
        }

        private void ButtonsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        private bool Loading = false;
        public void Load()
        {
            Loading = true;
            if (Project != null)
            {
                var packageFile = Path.Combine(Project.Directory, "config.json");
                if (File.Exists(packageFile))
                {
                    var configuration = JObject.Parse(File.ReadAllText(packageFile));
                    this.SetJSON(configuration, true);
                }
                else throw new Exception("No project configuration found in " + Project.Directory);
            }

            if (Mod != null)
            {
                if (System.IO.File.Exists(Mod.File))
                {
                    using (var assembly = AssemblyDefinition.ReadAssembly(Mod.File))
                    {
                        JObject configData = null;
                        foreach (var resource in assembly.MainModule.Resources)
                        {
                            if (resource is EmbeddedResource embedded && embedded.Name == "ModConfiguration")
                            {
                                configData = JObject.Parse(System.Text.Encoding.UTF8.GetString(embedded.GetResourceData()));
                                break;
                            }
                        }
                        if (configData == null)
                            throw new Exception("Mod at " + Mod.File + " is missing ModConfiguration resource.");
                        SetJSON(configData, true);
                    }
                }
            }
            Loading = false;
        }

        public void SetJSON(JObject configuration, bool projectConfiguration)
        {
            var buttons = projectConfiguration ? new ObservableCollection<ModButton>() : Buttons;
            if (projectConfiguration)
            {
                if (configuration.ContainsKey("name"))
                    Name = configuration["name"].ToString();
                else
                    throw new Exception("Name is missing in config.json in " + Project.Directory);
                if (configuration.ContainsKey("version"))
                    Version = new ModVersion(configuration["version"].ToString());
                else
                    throw new Exception("Version is missing in config.json in " + Project.Directory);

                if (Mod != null)
                {
                    MethodReplaces.Clear();
                    if (configuration.ContainsKey("method_replaces") && configuration["method_replaces"] is JArray methodReplaces)
                    {
                        for (var i = 0; i < methodReplaces.Count; i++)
                            MethodReplaces.Add(methodReplaces[i].ToString());
                    }
                    MethodHookAfter.Clear();
                    if (configuration.ContainsKey("method_hook_after") && configuration["method_hook_after"] is JArray methodHookAfter)
                    {
                        for (var i = 0; i < methodHookAfter.Count; i++)
                            MethodHookAfter.Add(methodHookAfter[i].ToString());
                    }
                    MethodHookBefore.Clear();
                    if (configuration.ContainsKey("method_hook_before") && configuration["method_hook_before"] is JArray methodHookBefore)
                    {
                        for (var i = 0; i < methodHookBefore.Count; i++)
                            MethodHookBefore.Add(methodHookBefore[i].ToString());
                    }
                    MethodChain.Clear();
                    if (configuration.ContainsKey("method_chain") && configuration["method_chain"] is JArray methodChain)
                    {
                        for (var i = 0; i < methodChain.Count; i++)
                            MethodChain.Add(methodChain[i].ToString());
                    }
                }
            }
            else
            {
                if (configuration.ContainsKey("is_activated"))
                    IsActivated = configuration["is_activated"].ToString().ToLowerInvariant() == "true";
            }
            if (configuration.ContainsKey("buttons") && configuration["buttons"] is JArray arr)
            {
                foreach (var el in arr)
                {
                    if (el is JObject obj)
                    {
                        if (!projectConfiguration)
                        {
                            foreach (var b in buttons)
                            {
                                if (b.ID == obj["id"].ToString())
                                {
                                    b.FromJSON(obj);
                                }
                            }
                        }
                        else
                        {
                            var button = new ModButton(this);
                            button.FromJSON(obj);
                            buttons.Add(button);
                        }
                    }
                }
            }
            Buttons = buttons;
        }

        public JObject ToJSON(bool modCompile = false)
        {
            var obj = new JObject();
            if (Mod != null)
            {
                obj["is_activated"] = IsActivated;
            }
            if (Project != null)
            {
                obj["name"] = Name;
                obj["version"] = Version.ToString();
                if (modCompile)
                {
                    var methodReplaces = new JArray();
                    foreach (var n in MethodReplaces)
                        methodReplaces.Add(n);
                    obj["method_replaces"] = methodReplaces;
                    var methodHookAfter = new JArray();
                    foreach (var n in MethodHookAfter)
                        methodHookAfter.Add(n);
                    obj["method_hook_after"] = methodHookAfter;
                    var methodHookBefore = new JArray();
                    foreach (var n in MethodHookBefore)
                        methodHookBefore.Add(n);
                    obj["method_hook_before"] = methodHookBefore;
                    var methodChain = new JArray();
                    foreach (var n in MethodChain)
                        methodChain.Add(n);
                    obj["method_chain"] = methodChain;
                }
            }
            var buttons = new JArray();
            foreach (var button in Buttons)
            {
                var bObj = button.ToJSON();
                buttons.Add(bObj);
            }
            obj["buttons"] = buttons;
            return obj;
        }

        public void Save()
        {
            if (Mod != null)
                Configuration.Save();
            if (Project == null)
                return;
            if (Loading)
                return;
            var packageFile = Path.Combine(Project.Directory, "config.json");
            var obj = ToJSON();
            File.WriteAllText(packageFile, obj.ToString());
        }
    }
}
