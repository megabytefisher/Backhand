using Backhand.Common.Pipelines;
using Backhand.Protocols.Cmp;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.Padp;
using Backhand.Protocols.Slp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Microsoft.Extensions.Logging;
using System.IO.Ports;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
    }).AddFilter((category, logLevel) =>
    {
        return true;
    });
});

var logger = loggerFactory.CreateLogger("test");

const string portName = "COM4";
const int initialBaud = 9600;

using SerialPort serialPort = new(portName)
{
    BaudRate = initialBaud,
    Handshake = Handshake.RequestToSend,
    Parity = Parity.None,
    StopBits = StopBits.One
};
serialPort.Open();

SerialPortPipe serialPipe = new(serialPort);

using SlpConnection slpConnection = new(serialPipe, logger: logger);
PadpConnection padpConnection = new PadpConnection(slpConnection, 3, 3);

// Start SLP IO
_ = slpConnection.RunIOAsync();

// Wait for wakeup packet
await CmpConnection.WaitForWakeUpAsync(padpConnection);

Console.WriteLine("Got wakeup");

// Do Handshake
await CmpConnection.DoHandshakeAsync(padpConnection);

Console.WriteLine("Handshake done");

DlpConnection dlpConnection = new DlpConnection(padpConnection);

ReadUserInfoResponse userInfo = await dlpConnection.ReadUserInfoAsync();

await dlpConnection.EndOfSyncAsync(new EndOfSyncRequest
{
    Status = EndOfSyncRequest.EndOfSyncStatus.Okay
});


Console.WriteLine(userInfo);