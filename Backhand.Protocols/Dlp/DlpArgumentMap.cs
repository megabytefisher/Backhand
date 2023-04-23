using System;
using System.Collections.Generic;
using Backhand.Common.BinarySerialization;

namespace Backhand.Protocols.Dlp
{
    public class DlpArgumentMap
    {
        public int Count => _dictionary.Count;

        private readonly Dictionary<DlpArgumentDefinition, IBinarySerializable> _dictionary;

        public DlpArgumentMap()
        {
            _dictionary = new Dictionary<DlpArgumentDefinition, IBinarySerializable>();
        }

        public void SetValue(DlpArgumentDefinition definition, IBinarySerializable value)
        {
            _dictionary.Add(definition, value);
        }

        public void SetValue<T>(DlpArgumentDefinition<T> definition, T value) where T : class, IBinarySerializable, new()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _dictionary.Add(definition, value);
        }

        public IBinarySerializable? GetValue(DlpArgumentDefinition definition)
        {
            return _dictionary.GetValueOrDefault(definition);
        }

        public T? GetValue<T>(DlpArgumentDefinition<T> definition) where T : class, IBinarySerializable, new()
        {
            return (T?)_dictionary.GetValueOrDefault(definition);
        }

        public ICollection<DlpArgumentDefinition> GetDefinitions()
        {
            return _dictionary.Keys;
        }

        public ICollection<IBinarySerializable> GetValues()
        {
            return _dictionary.Values;
        }
    }
}
