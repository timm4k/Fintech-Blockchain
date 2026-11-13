using System.Diagnostics;

namespace FintechBlockchain;

public sealed class MiningService
{
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(5);
    private readonly HashingService hashingService;

    public MiningService(HashingService hashingService)
    {
        ArgumentNullException.ThrowIfNull(hashingService);

        this.hashingService = hashingService;
    }

    public void MineBlock(Block block, Action<long, TimeSpan>? reportProgress = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentOutOfRangeException.ThrowIfLessThan(block.DifficultyAtMining, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(block.DifficultyAtMining, 64);
        string hashPrefix = new('0', block.DifficultyAtMining);
        string blockData = hashingService.GetBlockData(block);
        MiningResult result = Mine(nonce => hashingService.ComputeHash(blockData, nonce),
            hashPrefix, reportProgress);
        if (blockData != hashingService.GetBlockData(block))
        {
            throw new InvalidOperationException("Block contents changed during mining");
        }

        block.Nonce = result.Nonce;
        block.Hash = result.Hash;
        block.MiningAttempts = result.Attempts;
        block.MiningDuration = result.Elapsed;
    }

    public async Task<MiningResult> MineBlockAsync(
        Block block,
        int workerCount = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentOutOfRangeException.ThrowIfLessThan(block.DifficultyAtMining, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(block.DifficultyAtMining, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan(workerCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(workerCount, Math.Max(2, Environment.ProcessorCount));
        cancellationToken.ThrowIfCancellationRequested();

        string blockData = hashingService.GetBlockData(block);
        string prefix = new('0', block.DifficultyAtMining);
        MiningResult result = await Task.Run(
            () => SearchBlock(blockData, prefix, workerCount, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (blockData != hashingService.GetBlockData(block))
        {
            throw new InvalidOperationException("Block contents changed during mining");
        }
        block.Nonce = result.Nonce;
        block.Hash = result.Hash;
        block.MiningAttempts = result.Attempts;
        block.MiningDuration = result.Elapsed;
        return result;
    }

    private MiningResult SearchBlock(
        string blockData, string prefix, int workerCount, CancellationToken cancellationToken)
    {
        long[] attempts = new long[workerCount];
        MiningResult? winner = null;
        int activeWorkers = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = workerCount,
            CancellationToken = cancellationToken
        };
        var stopwatch = Stopwatch.StartNew();

        Parallel.For(0, workerCount, options, (worker, state) =>
        {
            long nonce = worker;
            long localAttempts = 0;
            Interlocked.Increment(ref activeWorkers);
            try
            {
                while (!state.IsStopped)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string hash = hashingService.ComputeHash(blockData, nonce);
                    localAttempts++;
                    if (hash.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        Interlocked.CompareExchange(
                            ref winner, new MiningResult(nonce, hash, 0, TimeSpan.Zero), null);
                        state.Stop();
                        break;
                    }
                    if (nonce > long.MaxValue - workerCount)
                    {
                        break;
                    }
                    nonce += workerCount;
                }
            }
            finally
            {
                attempts[worker] = localAttempts;
                Interlocked.Decrement(ref activeWorkers);
            }
        });

        stopwatch.Stop();
        cancellationToken.ThrowIfCancellationRequested();
        MiningResult found = winner ?? throw new InvalidOperationException("Nonce range exhausted");
        return found with
        {
            Attempts = attempts.Sum(),
            Elapsed = stopwatch.Elapsed,
            AttemptsPerWorker = Array.AsReadOnly(attempts),
            ActiveWorkersAtCompletion = activeWorkers
        };
    }

    public MiningResult MineText(string text, string alphabet, Action<long, TimeSpan>? reportProgress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(alphabet);
        if (alphabet.Length != 32 || alphabet.Distinct().Count() != 32)
        {
            throw new ArgumentException("The Base32 alphabet must contain 32 different characters.", nameof(alphabet));
        }
        string prefix = char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();
        if (prefix.Length >= 52 || !prefix.All(character => alphabet.Contains(char.ToLowerInvariant(character))))
        {
            throw new ArgumentException("Text contains unsupported characters or is too long for the hash prefix.", nameof(text));
        }

        return Mine(nonce => hashingService.ComputeLetterHash(text, nonce, alphabet), prefix, reportProgress);
    }

    private MiningResult Mine(Func<long, string> computeHash, string prefix, Action<long, TimeSpan>? reportProgress)
    {
        long nonce = 0;
        var stopwatch = Stopwatch.StartNew();
        TimeSpan nextProgress = ProgressInterval;

        while (true)
        {
            string hash = computeHash(nonce);
            long attempts = nonce + 1;

            if (hash.StartsWith(prefix, StringComparison.Ordinal))
            {
                return new MiningResult(nonce, hash, attempts, stopwatch.Elapsed);
            }

            if (reportProgress is not null && stopwatch.Elapsed >= nextProgress)
            {
                reportProgress(attempts, stopwatch.Elapsed);
                nextProgress = stopwatch.Elapsed + ProgressInterval;
            }

            nonce++;
        }
    }
}
