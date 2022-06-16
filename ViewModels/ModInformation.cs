using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.ViewModels
{
    public class ModInformation
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("ModInformation");
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

        public ModInformation(byte[] data)
        {
            this.FromJSON(JObject.Parse(System.Text.Encoding.UTF8.GetString(data)));
        }

        public ModInformation(string data)
        {
            this.FromJSON(JObject.Parse(data));
        }

        public ModInformation(JObject data)
        {
            this.FromJSON(data);
        }

        public ModInformation() { }

        public void FromJSON(JObject data)
        {
            if (data.ContainsKey("OriginalChecksum"))
                OriginalChecksum = data["OriginalChecksum"].ToString();
        }

        public JObject ToJSON()
        {
            var ret = new JObject();
            if (OriginalChecksum != null)
                ret["OriginalChecksum"] = OriginalChecksum;
            return ret;
        }
    }
}
