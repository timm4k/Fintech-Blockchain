namespace FintechBlockchain;

public sealed record MiningResult(long Nonce, string Hash, long Attempts, TimeSpan Elapsed);
