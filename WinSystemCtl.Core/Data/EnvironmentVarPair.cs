using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinSystemCtl.Core.Data
{
    public class EnvironmentVarPair : ViewModelBase, IComparable<EnvironmentVarPair>
    {
        private string _key;
        public string Key
        {
            get => _key;
            set => Set(ref _key, value);
        }

        private string _value;
        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public EnvironmentVarPair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public void Deconstruct(out string key, out string value)
        {
            key = Key;
            value = Value;
        }

        public int CompareTo(EnvironmentVarPair? other)
        {
            return Key.CompareTo(other?.Key);
        }
    }

    public class EnvironmentVarPairJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(EnvironmentVarPair);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, string>>(reader);
            if (dict == null)
                return new ObservableCollection<EnvironmentVarPair>();
            
            var collection = new ObservableCollection<EnvironmentVarPair>();
            foreach (var kvp in dict)
            {
                collection.Add(new EnvironmentVarPair(kvp.Key, kvp.Value));
            }
            return collection;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var collection = (ObservableCollection<EnvironmentVarPair>)value;
            var dict = collection.ToDictionary(x => x.Key, x => x.Value);
            serializer.Serialize(writer, dict);
        }
    }

}
