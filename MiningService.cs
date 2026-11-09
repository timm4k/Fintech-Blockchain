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
        MiningResult result = Mine(nonce =>
        {
            block.Nonce = nonce;
            return hashingService.ComputeHash(block);
        }, hashPrefix, reportProgress);

        block.Hash = result.Hash;
        block.MiningAttempts = result.Attempts;
        block.MiningDuration = result.Elapsed;
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
