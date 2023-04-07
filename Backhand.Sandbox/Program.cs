using Backhand.Common.BinarySerialization;
using Backhand.Common.Buffers;
using System.Buffers;

Test myTest = new Test
{
    TestValue = 5,
    BytesLength = 4
};

myTest.Bytes[0] = 1;
myTest.Bytes[1] = 2;
myTest.Bytes[2] = 3;
myTest.Bytes[3] = 4;

myTest.TestInner.TestValue = 0xAABBCCDD;

byte[] myArray = new byte[BinarySerializer<Test>.GetSize(myTest)];
SpanWriter<byte> myArrayWriter = new(myArray);

BinarySerializer<Test>.Serialize(myTest, ref myArrayWriter);

Test myNewTest = new Test();

SequenceReader<byte> myArrayReader = new(new ReadOnlySequence<byte>(myArray));
BinarySerializer<Test>.Deserialize(ref myArrayReader, myNewTest);


Console.WriteLine(BitConverter.ToString(myArray));

[BinarySerialized]
class Test
{
    [BinarySerialized]
    public int BytesLength
    {
        get => Bytes.Length;
        set => Bytes = new byte[value];
    }

    [BinarySerialized]
    public byte[] Bytes { get; private set; } = Array.Empty<byte>();

    [BinarySerialized]
    public TestInner TestInner { get; set; } = new();

    [BinarySerialized]
    public ushort TestValue { get; set; }
}

[BinarySerialized]
public class TestInner
{
    [BinarySerialized]
    public uint TestValue { get; set; }
}