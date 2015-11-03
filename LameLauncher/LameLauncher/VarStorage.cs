using System;
using System.Collections.Generic;
using System.Text;

namespace LameLauncher
{
	public class VarStorage
	{
		public Dictionary<string, string> data;

		public VarStorage ()
		{
			data = new Dictionary<string, string>();
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

		public string ProcessString(string str)
		{
			foreach(KeyValuePair<string, string> entry in data)
      {
				str = str.Replace("$" + entry.Key.ToUpper(), entry.Value);
      }
			return str;
		}
	}
}

