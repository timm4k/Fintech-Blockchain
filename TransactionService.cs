namespace FintechBlockchain;

public sealed class TransactionService
{
    public Transaction CreateTransaction(string from, string to, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        var transaction = new Transaction(Guid.NewGuid(), from.Trim(), to.Trim(), amount,
            "BTC", DateTime.UtcNow);
        Validate(transaction);
        return transaction;
    }

    public static void Validate(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentException.ThrowIfNullOrWhiteSpace(transaction.From);
        ArgumentException.ThrowIfNullOrWhiteSpace(transaction.To);
        ArgumentException.ThrowIfNullOrWhiteSpace(transaction.Asset);
        if (transaction.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero");
        }
        if (transaction.Id == Guid.Empty || transaction.TimeStamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Transaction requires an ID and a UTC timestamp");
        }
    }
}
