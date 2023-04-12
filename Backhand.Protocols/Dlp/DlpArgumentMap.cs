using System;
using System.Collections.Generic;

namespace Backhand.Protocols.Dlp
{
    public class DlpArgumentMap
    {
        public int Count => _dictionary.Count;

        private readonly Dictionary<DlpArgumentDefinition, DlpArgument> _dictionary;

        public DlpArgumentMap()
        {
            _dictionary = new Dictionary<DlpArgumentDefinition, DlpArgument>();
        }

        public void SetValue(DlpArgumentDefinition definition, DlpArgument value)
        {
            _dictionary.Add(definition, value);
        }

        public void SetValue<T>(DlpArgumentDefinition<T> definition, T value) where T : DlpArgument, new()
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            _dictionary.Add(definition, value);
        }

        public DlpArgument? GetValue(DlpArgumentDefinition definition)
        {
            return _dictionary.GetValueOrDefault(definition);
        }

        public T? GetValue<T>(DlpArgumentDefinition<T> definition) where T : DlpArgument, new()
        {
            return (T?)_dictionary.GetValueOrDefault(definition);
        }

        public ICollection<DlpArgumentDefinition> GetDefinitions()
        {
            return _dictionary.Keys;
        }

        public ICollection<DlpArgument> GetValues()
        {
            return _dictionary.Values;
        }
    }
}
