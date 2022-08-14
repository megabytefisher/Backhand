using System.IO.Pipelines;
using System.IO.Ports;

namespace Backhand.DeviceIO.Serial
{
    internal class SerialPortPipe : IDuplexPipe
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
