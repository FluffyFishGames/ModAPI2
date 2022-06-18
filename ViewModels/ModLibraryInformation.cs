using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.ViewModels
{
    public class ModLibraryInformation
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("ModLibraryInformation");
        private string _OriginalChecksum = null;
        public string OriginalChecksum
        {
            get
            {
                return _OriginalChecksum;
            }
            set
            {
                _OriginalChecksum = value;
            }
        }
        private int _OriginalFileSize = 0;
        public int OriginalFileSize
        {
            get
            {
                return _OriginalFileSize;
            }
            set
            {
                _OriginalFileSize= value;
            }
        }


        public ModLibraryInformation(byte[] data)
        {
            this.FromJSON(JObject.Parse(System.Text.Encoding.UTF8.GetString(data)));
        }

        public ModLibraryInformation(string data)
        {
            this.FromJSON(JObject.Parse(data));
        }

        public ModLibraryInformation(JObject data)
        {
            this.FromJSON(data);
        }

        public ModLibraryInformation() { }

        public void FromJSON(JObject data)
        {
            if (data.ContainsKey("OriginalChecksum"))
                OriginalChecksum = data["OriginalChecksum"].ToString();
            if (data.ContainsKey("OriginalFileSize") && int.TryParse(data["OriginalFileSize"].ToString(), out var fileSize))
                OriginalFileSize = fileSize;
        }

        public JObject ToJSON()
        {
            var ret = new JObject();
            if (OriginalChecksum != null)
                ret["OriginalChecksum"] = OriginalChecksum;
            ret["OriginalFileSize"] = OriginalFileSize;
            return ret;
        }
    }
}
