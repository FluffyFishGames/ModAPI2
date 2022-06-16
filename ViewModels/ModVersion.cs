using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace ModAPI.ViewModels
{

    public class ModVersion : ViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("ModVersion");
        private string _Version;
        private static Regex VersionRegex;

        public string Version
        {
            get
            {
                return _Version;
            }
            private set
            {
                if (_Version != value)
                {
                    _Version = value;
                    if (VersionRegex == null)
                        VersionRegex = new Regex(@"([0-9]+)\.?([0-9]+)?\.?([0-9]+)?");

                    var match = VersionRegex.Match(value);
                    var count = match.Groups.Count;
                    switch (count)
                    {
                        case 4:
                            _Build = int.Parse(match.Groups[3].Value);
                            this.RaisePropertyChanged<ModVersion>("Build");
                            goto case 3;
                        case 3:
                            _Minor = int.Parse(match.Groups[2].Value);
                            this.RaisePropertyChanged<ModVersion>("Minor");
                            goto case 2;
                        case 2:
                            _Major = int.Parse(match.Groups[1].Value);
                            this.RaisePropertyChanged<ModVersion>("Major");
                            break;
                    }
                    this.RaisePropertyChanged<ModVersion>("Version");
                }
            }
        }

        private int _Major;
        public int Major 
        { 
            get => _Major; 
            set 
            { 
                if (value != _Major)
                {
                    _Major = value;
                    _Version = _Major + "." + _Minor + "." + _Build;
                    this.RaisePropertyChanged<ModVersion>("Major");
                    this.RaisePropertyChanged<ModVersion>("Version");
                }
            }
        }
        private int _Minor;

        public int Minor
        {
            get => _Minor;
            set
            {
                if (value != _Minor)
                {
                    _Minor = value;
                    _Version = _Major + "." + _Minor + "." + _Build;
                    this.RaisePropertyChanged<ModVersion>("Minor");
                    this.RaisePropertyChanged<ModVersion>("Version");
                }
            }
        }
        private int _Build;
        public int Build
        {
            get => _Build;
            set
            {
                if (value != _Build)
                {
                    _Build = value;
                    _Version = _Major + "." + _Minor + "." + _Build;
                    this.RaisePropertyChanged<ModVersion>("Minor");
                    this.RaisePropertyChanged<ModVersion>("Version");
                }
            }
        }

        public ModVersion(string version)
        {
            Version = version;
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }
        public static bool operator <(ModVersion version1, ModVersion version2)
        {
            return version1.Major < version2.Major || version1.Minor < version2.Minor || version1.Build < version2.Build;
        }
        public static bool operator >(ModVersion version1, ModVersion version2)
        {
            return version1.Major > version2.Major || version1.Minor > version2.Minor || version1.Build > version2.Build;
        }
        public static bool operator <=(ModVersion version1, ModVersion version2)
        {
            return version1.Major < version2.Major || (version1.Major == version2.Major && (version1.Minor < version2.Minor || (version1.Minor == version2.Minor && version1.Build <= version2.Build)));
        }
        public static bool operator >=(ModVersion version1, ModVersion version2)
        {
            return version1.Major > version2.Major || (version1.Major == version2.Major && (version1.Minor > version2.Minor || (version1.Minor == version2.Minor && version1.Build >= version2.Build)));
        }
        public static bool operator ==(ModVersion version1, ModVersion version2)
        {
            if (Object.ReferenceEquals(version1, null) || Object.ReferenceEquals(version2, null))
                return Object.Equals(version1, version2);
            return version1.Major == version2.Major && version1.Minor == version2.Minor && version1.Build == version2.Build;
        }
        public static bool operator !=(ModVersion version1, ModVersion version2)
        {
            if (Object.ReferenceEquals(version1, null) || Object.ReferenceEquals(version2, null))
                return !Object.Equals(version1, version2);
            return version1.Major != version2.Major || version1.Minor != version2.Minor || version1.Build != version2.Build;
        }


        public override bool Equals(object obj)
        {
            if (obj is ModVersion v)
            {
                return Version == v.Version;
            }
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return Version;
        }
    }
}
