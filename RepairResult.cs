namespace FintechBlockchain;

public sealed record RepairResult(int BlocksRemined, long TotalAttempts, TimeSpan Elapsed);
