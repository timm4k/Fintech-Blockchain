using System.Text.Json.Serialization;

namespace FintechBlockchain;

public sealed class Block
{
    private IReadOnlyList<Transaction> transactions = Array.AsReadOnly(Array.Empty<Transaction>());

    public IReadOnlyList<Transaction> Transactions
    {
        get => transactions;
        init => transactions = Array.AsReadOnly(value.ToArray());
    }

    public int Index { get; init; }
    public DateTime TimeStamp { get; init; }
    public string Data { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    [JsonInclude]
    public string PreviousHash { get; internal set; } = string.Empty;
    public int DifficultyAtMining { get; init; } = 1;
    public long Nonce { get; set; }
    [JsonInclude]
    public long MiningAttempts { get; internal set; }
    [JsonInclude]
    public TimeSpan MiningDuration { get; internal set; }
    [JsonInclude]
    public string Hash { get; internal set; } = string.Empty;
}
