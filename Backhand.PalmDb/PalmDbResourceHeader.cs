namespace Backhand.PalmDb
{
    public record PalmDbResourceHeader
    {
        public required ushort Id { get; init; }
        public required string Type { get; init; }
    }
}