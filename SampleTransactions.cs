namespace FintechBlockchain;

public static class SampleTransactions
{
    private const int MinimumTransactionBlocks = 4;
    private const int MinimumTransactions = 12;

    public static bool AddMissing(BlockChain chain, TransactionService service, string author)
    {
        int confirmed = chain.Chain.Sum(block => block.Transactions.Count);
        int transactionBlocks = chain.Chain.Count(block => block.Transactions.Count > 0);
        if (confirmed >= MinimumTransactions && transactionBlocks >= MinimumTransactionBlocks)
            return false;

        var transfers = new (string From, string To, decimal Amount)[]
        {
            ("Nik", "Eva", 2m),
            ("Sofia", "Nik", 1m),
            ("Eva", "Mark", 0.2m),
            ("Mark", "Sofia", 0.5m),
            ("Nik", "Mark", 0.3m),
            ("Eva", "Sofia", 0.7m),
            ("Sofia", "Eva", 0.4m),
            ("Mark", "Nik", 0.6m),
            ("Nik", "Sofia", 1.2m),
            ("Eva", "Nik", 0.8m),
            ("Sofia", "Mark", 0.9m),
            ("Mark", "Eva", 0.25m)
        };
        int nextTransfer = 0;
        while (confirmed < MinimumTransactions || transactionBlocks < MinimumTransactionBlocks)
        {
            int count = Math.Min(3, Math.Max(1, MinimumTransactions - confirmed));
            Transaction[] group = transfers.Skip(nextTransfer).Take(count)
                .Select(transfer => service.CreateTransaction(transfer.From, transfer.To, transfer.Amount))
                .ToArray();
            chain.AddBlock(group, author);
            nextTransfer += group.Length;
            confirmed += group.Length;
            transactionBlocks++;
        }
        return true;
    }
}
