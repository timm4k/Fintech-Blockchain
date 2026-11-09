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

        var genesisBlock = new Block
        {
            Index = 0,
            TimeStamp = DateTime.UtcNow,
            Data = "Genesis Block",
            PreviousHash = "0",
            DifficultyAtMining = Difficulty
        };

        miningService.MineBlock(genesisBlock, reportProgress);
        blocks.Add(genesisBlock);
    }

    public void AddBlock(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        Block lastBlock = blocks[^1];
        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            TimeStamp = DateTime.UtcNow,
            Data = data,
            PreviousHash = lastBlock.Hash,
            DifficultyAtMining = Difficulty
        };

        miningService.MineBlock(newBlock, reportProgress);
        blocks.Add(newBlock);

        if (newBlock.Index % AdjustmentInterval == 0)
        {
            AdjustDifficulty();
        }
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
