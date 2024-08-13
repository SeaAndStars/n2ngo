using System.Collections.ObjectModel;
using System.Text;

namespace N2NGO.UtilsClass
{
    public class EasyConfig
    {
        public string CurrentConfigFile { get; private set; } = string.Empty;

        private Dictionary<string, string> _data = new Dictionary<string, string>();

        private bool ResolveConfig(string data, bool append = false)
        {
             if (!append)
                _data.Clear();

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

                _data.Add(key, value);
            }

            return true;
        }


        public bool LoadConfig(string filePath, bool create = true)
        {
            CurrentConfigFile = filePath;

            var filePathDirectory = Path.GetDirectoryName(CurrentConfigFile);
            if (filePathDirectory != null&& filePathDirectory!=string.Empty)
            {
                if (!Directory.Exists(filePathDirectory))
                    Directory.CreateDirectory(filePathDirectory);
            }
            if (create && !File.Exists(filePath))
                File.Create(filePath).Close();

            return ResolveConfig(File.ReadAllText(filePath));
        }


        public bool SaveConfigDataToFile()
        {
            StringBuilder sb = new();
            foreach (var cfg in _data)
            {
                sb.AppendFormat("{0} = {1}", cfg.Key, cfg.Value);
                sb.AppendLine();
            }
            File.WriteAllText(CurrentConfigFile, sb.ToString());

            return true;
        }


        public ReadOnlyDictionary<string, string> GetDataList() => new(_data);


        public EasyConfig() { }
        public EasyConfig(string filePath, bool create = true) => LoadConfig(filePath, create);


        public bool KeyExists(string key)
        {
            return _data.ContainsKey(key);
        }

        public string Get(string key, string default_)
        {
            if (KeyExists(key))
            { return _data[key]; }
            else
            {
                _data[key] = default_;
                return default_;
            }
        }
        public string? Get(string key)
        {
            if (KeyExists(key))
            { return _data[key]; }

            return null;
        }

        public void Set(string key, string value)
        {
            _data[key] = value;
        }

        public void Del(string key)
        {
            if (_data.ContainsKey(key)) { _data.Remove(key); }
        }

        public void Clear()
        {
            _data.Clear();
        }
    }
}
