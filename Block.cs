namespace FintechBlockchain;

public sealed class Block
{
    public int Index { get; init; }
    public DateTime TimeStamp { get; init; }
    public string Data { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string PreviousHash { get; internal set; } = string.Empty;
    public int DifficultyAtMining { get; init; } = 1;
    public long Nonce { get; set; }
    public long MiningAttempts { get; internal set; }
    public TimeSpan MiningDuration { get; internal set; }
    public string Hash { get; internal set; } = string.Empty;
}
