using System.Text;

namespace N2Nmc.UtilsClass
{
    public class EasyConfig
    {
        public string CurrentConfigFile { get; private set; } = string.Empty;
        
        private Dictionary<string, string> Data = new Dictionary<string, string>();

        private bool ResolveConfig(string data)
        {
            string[] configs = data.Split(Environment.NewLine);

            foreach (string cfg in configs)
            {
                string? key = null, value = null;

                try
                {
                    string[] _cfg = cfg.Split('=');
                    if (_cfg.Length != 2) continue;

                    _cfg[0] = _cfg[0].Trim();
                    _cfg[1] = _cfg[1].Trim();

                    key = _cfg[0];
                    value = _cfg[1];
                }
                catch { return false; }


                if (string.IsNullOrWhiteSpace(key))
                    continue;
                if (string.IsNullOrWhiteSpace(value))
                    value = "";

                Data.Add(key, value);
            }

            return true;
        }


        public bool LoadConfig(string filename, bool create = true)
        {
            CurrentConfigFile = filename;

            try
            {
                if (!File.Exists(filename)) 
                    File.Create(filename);

                return ResolveConfig(File.ReadAllText(filename));
            }
            catch { return false; }
        }


        public bool SaveConfigDataToFile()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var cfg in Data)
            {
                sb.AppendFormat("{0} = {1}", cfg.Key,cfg.Value);
                sb.AppendLine();
            }
            File.WriteAllText(CurrentConfigFile,sb.ToString());

            return true;
        }


        public Dictionary<string, string> GetDataList() => new Dictionary<string, string>(Data);


        public EasyConfig() { }
        public EasyConfig(string filename, bool create = true) => LoadConfig(filename, create);


        public bool KeyExists(string key)
        {
            return Data.ContainsKey(key);
        }

        public string Get(string key, string default_)
        {
            if (KeyExists(key))
            { return Data[key]; }
            else
            {
                Data[key] = default_;
                return default_;
            }
        }
        public string? Get(string key)
        {
            if (KeyExists(key))
            { return Data[key]; }

            return null;
        }

        public void Set(string key, string value)
        {
            Data[key] = value;
        }

        public void Del(string key)
        {
            if (Data.ContainsKey(key)) { Data.Remove(key); }
        }

        public void Clear()
        {
            Data.Clear();
        }
    }
}
