using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Serial
{
    internal class SerialPortPipe : IDuplexPipe
    {
        private SerialPort _serialPort;

        private PipeReader _inputReader;
        private PipeWriter _outputWriter;

        public SerialPortPipe(SerialPort serialPort)
        {
            _serialPort = serialPort;

            _inputReader = PipeReader.Create(_serialPort.BaseStream);
            _outputWriter = PipeWriter.Create(_serialPort.BaseStream);
        }

        public PipeReader Input => _inputReader;
        public PipeWriter Output => _outputWriter;
    }
}
