using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LameLauncher
{
    public class ConfigFile
    {
        public string path;
        public Dictionary<string, string> data;

        public void FlushConfig()
        {
            string text = "";
            foreach (KeyValuePair<string, string> pair in data)
            {
                text = text + pair.Key + " \"" + pair.Value.Replace("\"", "\\\"") + "\"";
                if (Updater.IsLoonix()) text = text + "\n";
                else text = text + "\r\n";
            }
            File.WriteAllText(path, text);
        }

        public ConfigFile(string INIPath)
        {
            path = INIPath;
            data = new Dictionary<string, string>();
            this.LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    List<string> results = Updater.TokenizeLine(line);
                    try
                    {
                        data.Add(results[0], results[1]);
                    }
                    catch (Exception e) { };
                }
            }
            catch (Exception e)
            {
            }
        }

        public void SetValue(string key, string value)
        {
            if (data.ContainsKey(key)) data[key] = value;
            else data.Add(key, value);
        }

        public string GetValue(string key, string def)
        {
            if (data.ContainsKey(key)) return data[key];
            SetValue(key, def);
            return def;
        }

		public string GetValue(string key, string def, VarStorage vars)
		{
			return vars.ProcessString(this.GetValue(key, def));
		}
    }
}
