namespace Backhand.PalmDb
{
    public record PalmDbRecordHeader
    {
        public required uint Id { get; init; }
        public required DatabaseRecordAttributes Attributes { get; init; }
        public required byte Category { get; init; }
        public required bool Archive { get; init; }
    }
}