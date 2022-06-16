using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System;
using ModAPI.ViewModels;

namespace ModAPI.ViewModels.ModProject
{

    public class ProjectFile
    {
        private ModProject _Project;
        private XDocument _Document;
        private FileInfo _File;
        private bool _UpdateRequired;
        private System.Version _Version;

        public System.Version Version
        {
            get
            {
                return _Version;
            }
            private set
            {
                _Version = value;
            }
        }

        public bool UpdateRequired
        {
            get
            {
                return _UpdateRequired;
            }
            set
            {
                _UpdateRequired = value;
            }
        }

        public ModProject Project
        {
            get
            {
                return _Project;
            }
            private set
            {
                _Project = value;
            }
        }

        public ProjectFile(ModProject project)
        {
            _Project = project;
            _File = new FileInfo(Path.Combine(project.Directory, "Project.csproj"));
            Load();
        }

        private static string ParseAssemblyName(string s)
        {
            foreach (var n in Path.GetInvalidPathChars())
                s = s.Replace(n + "", "");
            return s;
        }

        private void CheckVersion()
        {
            if (_Document.Root != null)
            {
                var itemGroups = _Document.Root.Elements("ItemGroup");
                foreach (var itemGroup in itemGroups)
                {
                    var references = itemGroup.Elements("Reference");
                    foreach (var reference in references)
                    {
                        var include = reference.Attribute("Include");
                        if (include != null)
                        {
                            if (include.Value.StartsWith("BaseModLib"))
                            {
                                var index = include.Value.IndexOf("Version=");
                                var comma = include.Value.IndexOf(",", index);
                                if (index > -1 && comma > -1)
                                {
                                    var versionValue = System.Version.Parse(include.Value.Substring(index + 8, comma - (index + 8)));
                                    _Version = versionValue;
                                    if (!versionValue.Equals(Data.ModAPI.BaseModLib.Name.Version))
                                    {
                                        _UpdateRequired = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Load()
        {
            if (_File.Exists)
            {
                _Document = XDocument.Parse(File.ReadAllText(_File.FullName));
                CheckVersion();
            }
            else
                _Document = new XDocument();
        }

        public void Save()
        {
            if (_Document.Root == null)
                _Document.Add(new XElement("Project"));

            _Document.Root.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");
            var propertyGroups = _Document.Root.Elements("PropertyGroup");
            bool targetFrameworkFound = false;

            XElement firstUncoditionalPropertyGroup = null;
            XElement debugAnyCPUPropertyGroup = null;
            XElement releaseAnyCPUPropertyGroup = null;
            foreach (var propertyGroup in propertyGroups)
            {
                var condition = propertyGroup.Attribute("Condition");
                if (condition == null)
                {
                    if (firstUncoditionalPropertyGroup == null)
                        firstUncoditionalPropertyGroup = propertyGroup;
                }
                else if (condition.Value == "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'")
                    debugAnyCPUPropertyGroup = propertyGroup;
                else if (condition.Value == "'$(Configuration)|$(Platform)'=='Release|AnyCPU'")
                    releaseAnyCPUPropertyGroup = propertyGroup;
            }

            if (debugAnyCPUPropertyGroup == null)
            {
                debugAnyCPUPropertyGroup = new XElement("PropertyGroup");
                debugAnyCPUPropertyGroup.SetAttributeValue("Condition", "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'");
                _Document.Root.Add(debugAnyCPUPropertyGroup);
            }
            if (releaseAnyCPUPropertyGroup == null)
            {
                releaseAnyCPUPropertyGroup = new XElement("PropertyGroup");
                releaseAnyCPUPropertyGroup.SetAttributeValue("Condition", "'$(Configuration)|$(Platform)'=='Release|AnyCPU'");
                _Document.Root.Add(releaseAnyCPUPropertyGroup);
            }
            var debugOutputPath = debugAnyCPUPropertyGroup.Element("OutputPath");
            if (debugOutputPath == null)
            {
                debugOutputPath = new XElement("OutputPath");
                debugAnyCPUPropertyGroup.Add(debugOutputPath);
            }
            debugOutputPath.SetValue("./");
            var releaseOutputPath = releaseAnyCPUPropertyGroup.Element("OutputPath");
            if (releaseOutputPath == null)
            {
                releaseOutputPath = new XElement("OutputPath");
                releaseAnyCPUPropertyGroup.Add(releaseOutputPath);
            }
            releaseOutputPath.SetValue("./");

            if (firstUncoditionalPropertyGroup == null)
            {
                firstUncoditionalPropertyGroup = new XElement("PropertyGroup");
                _Document.Root.Add(firstUncoditionalPropertyGroup);
            }
            var targetFramework = firstUncoditionalPropertyGroup.Element("TargetFramework");
            if (targetFramework == null)
            {
                targetFramework = new XElement("TargetFramework");
                firstUncoditionalPropertyGroup.Add(targetFramework);
            }
            targetFramework.SetValue("net45");

            var appendTargetFramework = firstUncoditionalPropertyGroup.Element("AppendTargetFrameworkToOutputPath");
            if (appendTargetFramework == null)
            {
                appendTargetFramework = new XElement("AppendTargetFrameworkToOutputPath");
                firstUncoditionalPropertyGroup.Add(appendTargetFramework);
            }
            appendTargetFramework.SetValue("false");

            var assemblyName = firstUncoditionalPropertyGroup.Element("AssemblyName");
            if (assemblyName == null)
            {
                assemblyName = new XElement("AssemblyName");
                firstUncoditionalPropertyGroup.Add(assemblyName);
            }
            assemblyName.SetValue(ParseAssemblyName(Project.Configuration.Name));

            Dictionary<string, Library> libraries = new Dictionary<string, Library>();
            HashSet<string> foundLibraries = new HashSet<string>();
            foreach (var library in Project.Game.ModLibrary.Libraries)
            {
                libraries.Add(Path.GetFileNameWithoutExtension(library.File), library);
            }

            var itemGroups = _Document.Root.Elements("ItemGroup");
            XElement libraryItemGroup = null;
            foreach (var itemGroup in itemGroups)
            {
                var attribute = itemGroup.Attribute("Label");
                if (attribute != null && attribute.Value == "ModLibrary")
                {
                    libraryItemGroup = itemGroup;
                    break;
                }
            }
            if (libraryItemGroup == null)
            {
                libraryItemGroup = new XElement("ItemGroup");
                libraryItemGroup.SetAttributeValue("Label", "ModLibrary");
                _Document.Root.Add(libraryItemGroup);
            }
            var references = libraryItemGroup.Elements("Reference");

            foreach (var reference in references)
            {
                var include = reference.Attribute("Include");
                if (include != null)
                {
                    var libraryName = include.Value;
                    var index = libraryName.IndexOf(",");
                    if (index > -1)
                        libraryName = libraryName.Substring(0, index);

                    libraryName = libraryName.Trim();
                    var privateElement = reference.Element("Private");
                    if (privateElement == null)
                        reference.Add(new XElement("Private", "false"));
                    else
                        privateElement.SetValue("false");

                    if (libraryName == "BaseModLib")
                    {
                        var hintPath = reference.Element("HintPath");
                        if (hintPath == null)
                        {
                            hintPath = new XElement("HintPath");
                            reference.Add(hintPath);
                        }
                        var specificVersion = reference.Element("SpecificVersion");
                        if (specificVersion == null)
                        {
                            specificVersion = new XElement("SpecificVersion");
                            reference.Add(specificVersion);
                        }
                        include.SetValue("BaseModLib, Version=" + Data.ModAPI.BaseModLib.Name.Version.ToString() + ", Culture=neutral, PublicKeyToken=null");
                        hintPath.SetValue(Path.Combine(Path.GetFullPath(Project.Game.ModLibrary.LibraryDirectory), "BaseModLib.dll"));
                        specificVersion.SetValue("true");
                        foundLibraries.Add("BaseModLib");
                    }
                    else if (libraries.ContainsKey(libraryName))
                    {
                        var hintPath = reference.Element("HintPath");
                        if (hintPath == null)
                        {
                            hintPath = new XElement("HintPath");
                            reference.Add(hintPath);
                        }
                        hintPath.SetValue(Path.GetFullPath(libraries[libraryName].File));
                        foundLibraries.Add(libraryName);
                    }
                }
            }

            foreach (var library in libraries)
            {
                if (!foundLibraries.Contains(library.Key))
                {
                    var newReference = new XElement("Reference");
                    newReference.SetAttributeValue("Include", library.Key);
                    var hintPath = new XElement("HintPath");
                    hintPath.SetValue(Path.GetFullPath(library.Value.File));
                    var @private = new XElement("Private", "false");
                    newReference.Add(hintPath);
                    newReference.Add(@private);

                    libraryItemGroup.Add(newReference);
                }
            }
            if (!foundLibraries.Contains("BaseModLib"))
            {
                var newReference = new XElement("Reference");
                newReference.SetAttributeValue("Include", "BaseModLib, Version=" + Data.ModAPI.BaseModLib.Name.Version.ToString());
                var hintPath = new XElement("HintPath");
                hintPath.SetValue(Path.Combine(Path.GetFullPath(Project.Game.ModLibrary.LibraryDirectory), "BaseModLib.dll"));
                var specificVersion = new XElement("SpecificVersion", "true");
                var @private = new XElement("Private", "false");
                newReference.Add(hintPath);
                newReference.Add(@private);
                newReference.Add(specificVersion);

                libraryItemGroup.Add(newReference);
            }

            File.WriteAllText(_File.FullName, _Document.ToString());
        }

        private static char[] Numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        /*private string GetNamespaceName()
        {
            var n = _Project.Name;
            if (Numbers.Contains(n[0]))
                n = "_" + n;
            n = n.Replace('.', '_');
            return n;
        }*/
    }

}
