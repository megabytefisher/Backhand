using System.Collections.Generic;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpArgumentCollection
    {
        public int Count => _values.Count;

        private readonly Dictionary<DlpArgumentDefinition, DlpArgument> _values;

        public DlpArgumentCollection()
        {
            _values = new Dictionary<DlpArgumentDefinition, DlpArgument>();
        }

        public void SetValue(DlpArgumentDefinition argument, DlpArgument value)
        {
            _values[argument] = value;
        }

        public DlpArgument? GetValue(DlpArgumentDefinition argument)
        {
            return _values.TryGetValue(argument, out DlpArgument? value) ? value : null;
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
