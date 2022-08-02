using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpArgumentCollection
    {
        public int Count => _values.Count;

        private Dictionary<DlpArgumentDefinition, DlpArgument> _values;

        public DlpArgumentCollection()
        {
            _values = new Dictionary<DlpArgumentDefinition, DlpArgument>();
        }

        public void SetValue(DlpArgumentDefinition argument, DlpArgument value)
        {
            if (value != null)
                _values[argument] = value;
        }

        public DlpArgument? GetValue(DlpArgumentDefinition argument)
        {
            if (!_values.TryGetValue(argument, out DlpArgument? value))
                return null;

            return value;
        }

        public T? GetValue<T>(DlpArgumentDefinition<T> argument)
            where T : DlpArgument, new()
        {
            if (!_values.TryGetValue(argument, out DlpArgument? value))
                return null;

            return (T?)value;
        }

        public IEnumerable<DlpArgumentDefinition> GetDefinitions()
        {
            return _values.Keys;
        }

        public IEnumerable<DlpArgument> GetValues()
        {
            return _values.Values;
        }
    }
}
