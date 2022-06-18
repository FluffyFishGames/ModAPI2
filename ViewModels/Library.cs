using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ModAPI.Data;
using ModAPI.Utils;
using Mono.Cecil;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class Library : ViewModelBase
    {

        private bool _IsOutdated;
        public bool IsOutdated { get => _IsOutdated; set => this.RaiseAndSetIfChanged<Library, bool>(ref _IsOutdated, value, "IsOutdated"); }

        private bool _IsModded;
        public bool IsModded { get => _IsModded; set => this.RaiseAndSetIfChanged<Library, bool>(ref _IsModded, value, "IsModded"); }

        private bool _IsMod;
        public bool IsMod { get => _IsMod; set => this.RaiseAndSetIfChanged<Library, bool>(ref _IsMod, value, "IsMod"); }
        private bool _IsSystem;
        public bool IsSystem { get => _IsSystem; set => this.RaiseAndSetIfChanged<Library, bool>(ref _IsSystem, value, "IsSystem"); }

        private string _File;
        public string File { get => _File; set => this.RaiseAndSetIfChanged<Library, string>(ref _File, value, "File"); }

        private Game _Game;
        public Game Game { get => _Game; set => this.RaiseAndSetIfChanged<Library, Game>(ref _Game, value, "Game"); }

        private ModLibraryInformation _ModInformation;
        public ModLibraryInformation ModInformation { get => _ModInformation; set => this.RaiseAndSetIfChanged<Library, ModLibraryInformation>(ref _ModInformation, value, "ModInformation"); }

        
        public AssemblyDefinition LoadAssembly(ReadingMode readingMode = ReadingMode.Deferred)
        {
            AssemblyDefinition assembly = null;
            if (!System.IO.File.Exists(File))
                throw new FileNotFoundException("Assembly " + Path.GetFullPath(File) + " was not found.");
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(File);
            assembly = AssemblyDefinition.ReadAssembly(File, new ReaderParameters()
            {
                AssemblyResolver = resolver,
                ReadingMode = readingMode
            });
            return assembly;
        }

        public Library(Game game, string file)
        {
            Game = game;
            File = file;
            CheckIfModded();
        }

        private void CheckIfModded()
        {
            var assembly = LoadAssembly();

            if (assembly.Name.Name.StartsWith("System") || assembly.Name.Name == "mscorlib" || assembly.Name.Name.StartsWith("Mono") || assembly.Name.Name == "mcs" || assembly.Name.Name == "Mono" || assembly.Name.Name == "Boo")
                _IsSystem = true;
            try
            {
                if (assembly.MainModule.HasResources)
                {
                    foreach (var resource in assembly.MainModule.Resources)
                    {
                        if (resource.ResourceType == ResourceType.Embedded && resource.Name == "ModInformation" && resource is EmbeddedResource embedded)
                            ModInformation = new ModLibraryInformation(embedded.GetResourceData());
                        if (resource.ResourceType == ResourceType.Embedded && resource.Name == "ModConfiguration")
                            _IsMod = true;
                    }
                }

                foreach (var reference in assembly.MainModule.AssemblyReferences)
                {
                    if (reference.Name == "BaseModLib")
                    {
                        _IsModded = true;
                        _IsOutdated = !reference.Version.Equals(Data.ModAPI.BaseModLib.Name.Version);
                        return;
                    }
                }
                _IsModded = false;
            }
            finally
            {
                assembly.MainModule.Dispose();
                assembly.Dispose();
                assembly = null;
            }
        }

        private void LoadModConfiguration()
        {
            var assembly = LoadAssembly();
            if (assembly.MainModule.HasResources)
            {
                foreach (var resource in assembly.MainModule.Resources)
                {
                    if (resource.ResourceType == ResourceType.Embedded && resource.Name == "ModInformation" && resource is EmbeddedResource embedded)
                    {
                        _ModInformation = new ModLibraryInformation(embedded.GetResourceData());
                        return;
                    }
                }
            }

            assembly.MainModule.Dispose();
            assembly.Dispose();
            assembly = null;
        }

        public void Dispose()
        {
        }

        const int HashParts = 2;
        public string GetOriginalChecksum()
        {
            if (ModInformation != null)
                return ModInformation.OriginalChecksum;

            byte[] data = System.IO.File.ReadAllBytes(File);
            return Utils.Checksum.Create(data);
        }
    }
}
