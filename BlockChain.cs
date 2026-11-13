using System.Diagnostics;

namespace FintechBlockchain;

public sealed class BlockChain
{
    private readonly List<Block> blocks = [];
    private readonly HashingService hashingService = new();
    private readonly MiningService miningService;
    private readonly Action<long, TimeSpan>? reportProgress;

    public IReadOnlyList<Block> Chain { get; }
    public int Difficulty { get; private set; }
    public int AdjustmentInterval { get; }
    public TimeSpan TargetBlockTime { get; }
    public TimeSpan LastAverageMiningTime { get; private set; }

    public BlockChain(
        int difficulty = 1,
        Action<long, TimeSpan>? reportProgress = null,
        int adjustmentInterval = 3,
        TimeSpan? targetBlockTime = null)
        : this(difficulty, reportProgress, adjustmentInterval, targetBlockTime, true)
    {
    }

    private BlockChain(int difficulty, Action<long, TimeSpan>? reportProgress,
        int adjustmentInterval, TimeSpan? targetBlockTime, bool createGenesis)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(difficulty, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(difficulty, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan(adjustmentInterval, 1);
        TimeSpan target = targetBlockTime ?? TimeSpan.FromSeconds(0.5);
        if (target <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(targetBlockTime), "Target block time must be positive.");
        }

        AdjustmentInterval = adjustmentInterval;
        TargetBlockTime = target;
        miningService = new MiningService(hashingService);
        Difficulty = difficulty;
        this.reportProgress = reportProgress;
        Chain = blocks.AsReadOnly();

        if (!createGenesis) return;

        var genesisBlock = new Block
        {
            Index = 0,
            TimeStamp = DateTime.UtcNow,
            Data = "Genesis Block",
            Author = hashingService.ComputeHash("author:genesis"),
            PreviousHash = "0",
            DifficultyAtMining = Difficulty
        };

        miningService.MineBlock(genesisBlock, reportProgress);
        blocks.Add(genesisBlock);
    }

    public void AddBlock(string data)
    {
        AddBlock(data, hashingService.ComputeHash("author:anonymous"));
    }

    public void AddBlock(string data, string author)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);

        Block lastBlock = blocks[^1];
        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            TimeStamp = DateTime.UtcNow,
            Data = data,
            Author = author,
            PreviousHash = lastBlock.Hash,
            DifficultyAtMining = Difficulty
        };

        AppendBlock(newBlock);
    }

    public void AddBlock(IEnumerable<Transaction> transactions, string author)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        if (!IsValid()) throw new InvalidOperationException("Repair the chain before adding a block");
        Transaction[] snapshot = transactions.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("No pending transactions");
        foreach (Transaction transaction in snapshot) TransactionService.Validate(transaction);
        var knownIds = blocks.SelectMany(block => block.Transactions).Select(tx => tx.Id).ToHashSet();
        foreach (Transaction transaction in snapshot)
        {
            if (!knownIds.Add(transaction.Id)) throw new ArgumentException("Duplicate transaction ID");
        }
        var block = new Block
        {
            Index = blocks.Count,
            TimeStamp = DateTime.UtcNow,
            Data = "Transactions",
            Author = author,
            PreviousHash = blocks[^1].Hash,
            DifficultyAtMining = Difficulty,
            Transactions = snapshot
        };
        AppendBlock(block);
    }

    private void AppendBlock(Block block)
    {
        miningService.MineBlock(block, reportProgress);
        blocks.Add(block);
        if (block.Index % AdjustmentInterval == 0) AdjustDifficulty();
    }

    public static BlockChain Restore(IReadOnlyList<Block> savedBlocks, int difficulty,
        int adjustmentInterval, TimeSpan targetBlockTime, Action<long, TimeSpan>? reportProgress = null)
    {
        if (savedBlocks.Count == 0) throw new ArgumentException("Stored chain has no genesis block");
        var chain = new BlockChain(difficulty, reportProgress, adjustmentInterval, targetBlockTime, false);
        foreach (Block block in savedBlocks)
        {
            if (block is null || string.IsNullOrWhiteSpace(block.Hash)
                || string.IsNullOrWhiteSpace(block.PreviousHash)
                || string.IsNullOrWhiteSpace(block.Author) || block.Data is null
                || block.TimeStamp.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("Stored block has missing or invalid fields");
        }
        chain.blocks.AddRange(savedBlocks);
        var ids = new HashSet<Guid>();
        foreach (Transaction transaction in savedBlocks.SelectMany(block => block.Transactions))
        {
            TransactionService.Validate(transaction);
            if (!ids.Add(transaction.Id)) throw new ArgumentException("Duplicate stored transaction ID");
        }
        InvalidBlockResult? invalid = chain.FindFirstInvalidBlock();
        if (invalid is not null)
            throw new InvalidDataException($"Stored block {invalid.Index}: {invalid.Reason}");
        return chain;
    }

    private void AdjustDifficulty()
    {
        double averageTicks = blocks.TakeLast(AdjustmentInterval)
            .Average(block => (double)block.MiningDuration.Ticks);
        LastAverageMiningTime = TimeSpan.FromTicks((long)averageTicks);

        if (averageTicks < TargetBlockTime.Ticks / 2.0)
        {
            Difficulty = Math.Min(64, Difficulty + 1);
        }
        else if (averageTicks > TargetBlockTime.Ticks * 2.0)
        {
            Difficulty = Math.Max(1, Difficulty - 1);
        }
    }

    public Block? FindBlockByIndex(int index)
    {
        return index >= 0 && index < blocks.Count ? blocks[index] : null;
    }

    public IReadOnlyList<Block> FindBlocksByData(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return blocks
            .Where(block => block.Data.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public (Transaction Transaction, int BlockIndex)? FindTransactionById(Guid id)
    {
        foreach (Block block in blocks)
        {
            Transaction? transaction = block.Transactions.FirstOrDefault(tx => tx.Id == id);
            if (transaction is not null) return (transaction, block.Index);
        }
        return null;
    }

    public IReadOnlyList<(Transaction Transaction, int BlockIndex)> FindTransactionsByUser(
        string user, TransactionDirection direction = TransactionDirection.All)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        user = user.Trim();
        var results = new List<(Transaction Transaction, int BlockIndex)>();
        foreach (Block block in blocks)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                bool isSender = string.Equals(transaction.From, user, StringComparison.OrdinalIgnoreCase);
                bool isRecipient = string.Equals(transaction.To, user, StringComparison.OrdinalIgnoreCase);
                bool matches = direction switch
                {
                    TransactionDirection.Sent => isSender,
                    TransactionDirection.Received => isRecipient,
                    _ => isSender || isRecipient
                };
                if (matches) results.Add((transaction, block.Index));
            }
        }
        return results;
    }

    public InvalidBlockResult? FindFirstInvalidBlock()
    {
        for (int index = 0; index < blocks.Count; index++)
        {
            Block block = blocks[index];
            string expectedPreviousHash = index == 0 ? "0" : blocks[index - 1].Hash;
            List<string> reasons = [];

            if (block.Index != index)
            {
                reasons.Add("Incorrect block index");
            }
            if (block.Hash != hashingService.ComputeHash(block))
            {
                reasons.Add("Incorrect Hash");
            }
            if (block.PreviousHash != expectedPreviousHash)
            {
                reasons.Add("Broken PreviousHash");
            }
            if (block.DifficultyAtMining is < 1 or > 64 ||
                !block.Hash.StartsWith(new string('0', block.DifficultyAtMining), StringComparison.Ordinal))
            {
                reasons.Add("Invalid Proof of Work");
            }

            if (reasons.Count > 0)
            {
                return new InvalidBlockResult(index, string.Join(" | ", reasons));
            }
        }

        return null;
    }

    public bool IsValid()
    {
        return FindFirstInvalidBlock() is null;
    }

    public bool IsChainValid()
    {
        return IsValid();
    }

    public RepairResult RepairChain(int startIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, blocks.Count);
        InvalidBlockResult? invalid = FindFirstInvalidBlock();
        if (invalid is not null && invalid.Index < startIndex)
        {
            throw new InvalidOperationException($"Repair must start at or before invalid block {invalid.Index}.");
        }
        for (int index = startIndex; index < blocks.Count; index++)
        {
            if (blocks[index].Index != index || blocks[index].DifficultyAtMining is < 1 or > 64)
            {
                throw new InvalidOperationException("Repair requires valid block indices and original mining difficulties.");
            }
        }

        long attempts = 0;
        var stopwatch = Stopwatch.StartNew();
        for (int index = startIndex; index < blocks.Count; index++)
        {
            Block block = blocks[index];
            block.PreviousHash = index == 0 ? "0" : blocks[index - 1].Hash;
            miningService.MineBlock(block, reportProgress);
            attempts = checked(attempts + block.MiningAttempts);
        }
        stopwatch.Stop();

        return new RepairResult(blocks.Count - startIndex, attempts, stopwatch.Elapsed);
    }
}
