using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core.Data;

namespace WinSystemCtl.Core
{
    internal static class Utils
    {
        public static ObservableCollection<EnvironmentVarPair> LoadEnvironmentVars(string file)
        {
            var result = new ObservableCollection<EnvironmentVarPair>();

            using (var reader = new System.IO.StreamReader(file, detectEncodingFromByteOrderMarks: true))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        continue;
                    var parts = line.Split("=");
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        result.Add(new(key, value));
                    }
                }
            }

            return result;
        }

        public static void ConcatInplace<T>(ICollection<T> collection1, ICollection<T> collection2)
        {
            foreach (var item in collection2)
            {
                collection1.Add(item);
            }
        }

        public static void UpdateEnvironmentVariables(IDictionary<string, string?> envs, ObservableCollection<EnvironmentVarPair> newEnvs)
        {
            foreach (var (key, value) in newEnvs)
            {
                if(envs.ContainsKey(key))
                {
                    envs[key] = value;
                }
                else
                {
                    envs.Add(key, value);
                }
            }
        }
    }
}
