using System.IO.Pipelines;
using System.IO.Ports;

namespace Backhand.Common.Pipelines
{
    public class SerialPortPipe : IDuplexPipe
    {
        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        public SerialPortPipe(SerialPort serialPort)
        {
            Input = PipeReader.Create(serialPort.BaseStream);
            Output = PipeWriter.Create(serialPort.BaseStream);
        }
    }
}
