using System.Text.Json;

namespace FintechBlockchain;

public sealed class BlockchainStorage(string path)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public (BlockChain Chain, List<Transaction> Pending) Load(Action<long, TimeSpan> reportProgress)
    {
        if (!File.Exists(path)) return (new BlockChain(2, reportProgress), []);
        using FileStream stream = File.OpenRead(path);
        StoredLedger ledger = JsonSerializer.Deserialize<StoredLedger>(stream, Options)
            ?? throw new InvalidDataException("Storage is empty");
        if (ledger.Blocks is null || ledger.Blocks.Any(block => block is null))
            throw new InvalidDataException("Storage requires blocks");
        BlockChain chain = BlockChain.Restore(ledger.Blocks, ledger.Difficulty,
            ledger.AdjustmentInterval, ledger.TargetBlockTime, reportProgress);
        List<Transaction> pending = ledger.Pending ?? [];
        ValidatePending(chain, pending);
        return (chain, pending);
    }

    public void Save(BlockChain chain, IReadOnlyList<Transaction> pending)
    {
        InvalidBlockResult? invalid = chain.FindFirstInvalidBlock();
        if (invalid is not null)
            throw new InvalidOperationException($"Cannot save block {invalid.Index}: {invalid.Reason}");
        ValidatePending(chain, pending);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + ".tmp";
        var ledger = new StoredLedger(chain.Chain.ToList(), chain.Difficulty,
            chain.AdjustmentInterval, chain.TargetBlockTime, pending.ToList());
        using (FileStream stream = File.Create(temporaryPath))
        {
            JsonSerializer.Serialize(stream, ledger, Options);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporaryPath, fullPath, overwrite: true);
    }

    private static void ValidatePending(BlockChain chain, IEnumerable<Transaction> pending)
    {
        var ids = chain.Chain.SelectMany(block => block.Transactions).Select(tx => tx.Id).ToHashSet();
        foreach (Transaction transaction in pending)
        {
            TransactionService.Validate(transaction);
            if (!ids.Add(transaction.Id)) throw new ArgumentException("Duplicate transaction ID");
        }
    }

    private sealed record StoredLedger(List<Block> Blocks,
        int Difficulty, int AdjustmentInterval, TimeSpan TargetBlockTime,
        List<Transaction>? Pending = null);
}
