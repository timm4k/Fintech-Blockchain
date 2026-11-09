namespace FintechBlockchain;

public sealed class BlockChainDisplayService
{
    public void DisplayChain(BlockChain blockchain)
    {
        Console.WriteLine($"BLOCKCHAIN | Next difficulty: {blockchain.Difficulty} | Blocks: {blockchain.Chain.Count} | Genesis included");
        Console.WriteLine();

        foreach (Block block in blockchain.Chain)
        {
            DisplayBlock(block);
        }
    }

    public void DisplayBlock(Block block)
    {
        Console.WriteLine($"Index:        {block.Index}");
        Console.WriteLine($"TimeStamp:    {block.TimeStamp:O}");
        Console.WriteLine($"Data:         {block.Data}");
        Console.WriteLine($"PreviousHash: {block.PreviousHash}");
        Console.WriteLine($"Difficulty:   {block.DifficultyAtMining}");
        Console.WriteLine($"Nonce:        {block.Nonce}");
        Console.WriteLine($"Hash:         {block.Hash}");
        Console.WriteLine($"Attempts:     {block.MiningAttempts}");
        Console.WriteLine($"Mining time:  {block.MiningDuration.TotalMilliseconds:F3} ms");
        Console.WriteLine(new string('-', 78));
    }

    public void DisplayDifficultyAdjustment(BlockChain blockchain)
    {
        int previousDifficulty = blockchain.Chain[^1].DifficultyAtMining;
        Console.WriteLine($"DIFFICULTY REVIEW | Last {blockchain.AdjustmentInterval} blocks");
        Console.WriteLine($"Average mining time: {blockchain.LastAverageMiningTime.TotalMilliseconds:F3} ms | Target: {blockchain.TargetBlockTime.TotalMilliseconds:F0} ms");
        Console.WriteLine($"Difficulty: {previousDifficulty} -> {blockchain.Difficulty} (next block)");
        Console.WriteLine();
    }

    public void PrintDifficultyHistory(BlockChain blockchain)
    {
        Console.WriteLine("DIFFICULTY HISTORY");
        Console.WriteLine($"{"Index",-8}{"DifficultyAtMining",-22}{"Attempts",-16}{"MiningDuration (ms)"}");
        foreach (Block block in blockchain.Chain)
        {
            Console.WriteLine($"{block.Index,-8}{block.DifficultyAtMining,-22}{block.MiningAttempts,-16}{block.MiningDuration.TotalMilliseconds:F3}");
        }
        Console.WriteLine();
    }

    public void DisplayDifficultyImpact(IEnumerable<Block> blocks)
    {
        Console.WriteLine("REPAIR COST");
        Console.WriteLine($"{"Difficulty",-14}{"Blocks",-10}{"Avg attempts",-18}{"Avg ms",-14}{"Attempts ratio"}");
        double? previousAverage = null;
        foreach (var group in blocks.GroupBy(block => block.DifficultyAtMining).OrderBy(group => group.Key))
        {
            double averageAttempts = group.Average(block => (double)block.MiningAttempts);
            double averageTime = group.Average(block => block.MiningDuration.TotalMilliseconds);
            string ratio = previousAverage is null ? "-" : $"{averageAttempts / previousAverage.Value:F2}";
            Console.WriteLine($"{group.Key,-14}{group.Count(),-10}{averageAttempts,-18:F1}{averageTime,-14:F3}{ratio}");
            previousAverage = averageAttempts;
        }
        Console.WriteLine();
    }

    public void DisplayValidation(BlockChain blockchain, string stage)
    {
        InvalidBlockResult? invalid = blockchain.FindFirstInvalidBlock();
        string index = invalid?.Index.ToString() ?? "-";
        string reason = invalid?.Reason ?? "-";
        Console.WriteLine($"{stage,-18}{index,-16}{reason,-25}{invalid is null}");
    }

    public void DisplayRepairResult(RepairResult result, BlockChain blockchain, int preservedDifficulties)
    {
        Console.WriteLine("REPAIR");
        Console.WriteLine($"{"Blocks",-10}{"Attempts",-16}{"Time (ms)",-16}{"Difficulty kept",-20}{"IsValid"}");
        string preserved = $"{preservedDifficulties}/{blockchain.Chain.Count}";
        Console.WriteLine($"{result.BlocksRemined,-10}{result.TotalAttempts,-16}{result.Elapsed.TotalMilliseconds,-16:F3}{preserved,-20}{blockchain.IsValid()}");
        Console.WriteLine();
    }

    public void DisplayMiningProgress(long attempts, TimeSpan elapsed)
    {
        Console.WriteLine($"Mining... Attempts: {attempts} | Elapsed: {elapsed.TotalSeconds:F1} s");
    }

    public void DisplayMiningResult(MiningResult result)
    {
        Console.WriteLine($"Hash:         {result.Hash}");
        Console.WriteLine($"Nonce:        {result.Nonce}");
        Console.WriteLine($"Attempts:     {result.Attempts}");
        Console.WriteLine($"Mining time:  {result.Elapsed.TotalSeconds:F3} s");
        Console.WriteLine();
    }

    public void DisplayComparison(IEnumerable<BlockChain> blockchains)
    {
        Console.WriteLine("MINING COMPARISON");
        Console.WriteLine($"{"Difficulty",-12}{"Blocks",-8}{"Attempts",-16}{"Time (ms)",-16}{"Valid"}");
        foreach (BlockChain blockchain in blockchains)
        {
            long attempts = blockchain.Chain.Sum(block => block.MiningAttempts);
            var elapsed = TimeSpan.FromTicks(blockchain.Chain.Sum(block => block.MiningDuration.Ticks));
            Console.WriteLine($"{blockchain.Difficulty,-12}{blockchain.Chain.Count,-8}{attempts,-16}{elapsed.TotalMilliseconds,-16:F3}{blockchain.IsChainValid()}");
        }
        Console.WriteLine();
    }
}
