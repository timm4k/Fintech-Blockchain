using System.Globalization;

namespace FintechBlockchain;

public sealed class BlockchainMenu
{
    private readonly BlockchainStorage storage;
    private readonly BlockChainDisplayService display = new();
    private readonly TransactionService transactions = new();
    private readonly BlockChain chain;
    private readonly List<Transaction> pending;
    private bool hasUnsavedChanges;
    private readonly string author = new HashingService().ComputeHash("author:001");

    public BlockchainMenu(BlockchainStorage storage)
    {
        this.storage = storage;
        (chain, pending) = storage.Load(display.DisplayMiningProgress);
        hasUnsavedChanges = SampleTransactions.AddMissing(chain, transactions, author);
        SaveChanges();
    }

    public void Run()
    {
        Console.WriteLine("BLOCKCHAIN TRANSACTIONS");
        Console.WriteLine($"Blocks: {chain.Chain.Count} | Confirmed: {chain.Chain.Sum(block => block.Transactions.Count)} | Pending: {pending.Count}");
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1 - Add transaction");
            Console.WriteLine("2 - Add block");
            Console.WriteLine("3 - Show transaction history");
            Console.WriteLine("4 - Show blockchain");
            Console.WriteLine("5 - Validate blockchain");
            Console.WriteLine("6 - Find transaction by ID");
            Console.WriteLine("7 - Find transactions by user");
            Console.WriteLine("0 - Exit");
            Console.Write("Choose an option: ");
            string? option = Console.ReadLine()?.Trim();
            try
            {
                SaveChanges();
                switch (option)
                {
                    case null:
                    case "0":
                        return;
                    case "1":
                        CreateTransaction();
                        break;
                    case "2":
                        AddBlock();
                        break;
                    case "3":
                        ShowHistory();
                        break;
                    case "4":
                        display.DisplayChain(chain);
                        break;
                    case "5":
                        ShowValidation();
                        break;
                    case "6":
                        FindTransactionById();
                        break;
                    case "7":
                        FindTransactionsByUser();
                        break;
                    default:
                        Console.WriteLine("Choose a number from 0 to 7");
                        break;
                }
            }
            catch (EndOfStreamException)
            {
                return;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                Console.WriteLine($"Cannot complete operation: {exception.Message}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.WriteLine($"History was not saved: {exception.Message}");
                Console.WriteLine("Current data remains in memory, saving will retry with the next action");
                if (option is null) throw;
            }
        }
    }

    private void CreateTransaction()
    {
        string from = ReadRequired("From: ");
        string to = ReadRequired("To: ");
        decimal amount;
        while (true)
        {
            string input = ReadRequired("Amount in BTC (for example 0.2 or 0,2): ");
            if (decimal.TryParse(input.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out amount) && amount > 0) break;
            Console.WriteLine("Enter a positive amount without thousands separators");
        }
        Transaction transaction = transactions.CreateTransaction(from, to, amount);
        pending.Add(transaction);
        hasUnsavedChanges = true;
        SaveChanges();
        Console.WriteLine($"Transaction added | Pending: {pending.Count}");
        display.DisplayTransactions([transaction]);
    }

    private void AddBlock()
    {
        if (pending.Count == 0)
        {
            Console.WriteLine("Add a transaction before creating a block");
            return;
        }
        chain.AddBlock(pending, author);
        pending.Clear();
        hasUnsavedChanges = true;
        SaveChanges();
        Block block = chain.Chain[^1];
        Console.WriteLine($"Block {block.Index} added | Transactions: {block.Transactions.Count} | Pending: {pending.Count}");
        display.DisplayBlock(block);
    }

    private void SaveChanges()
    {
        if (!hasUnsavedChanges) return;
        storage.Save(chain, pending);
        hasUnsavedChanges = false;
    }

    private void ShowValidation()
    {
        InvalidBlockResult? invalid = chain.FindFirstInvalidBlock();
        Console.WriteLine($"IsValid: {invalid is null}");
        if (invalid is not null)
            Console.WriteLine($"Block: {invalid.Index} | Reason: {invalid.Reason}");
    }

    private void ShowHistory()
    {
        Console.WriteLine($"TRANSACTION HISTORY | Confirmed: {chain.Chain.Sum(block => block.Transactions.Count)} | Pending: {pending.Count}");
        Console.WriteLine("CONFIRMED");
        display.DisplayTransactions(chain.Chain.SelectMany(block => block.Transactions));
        Console.WriteLine("PENDING");
        display.DisplayTransactions(pending);
    }

    private void FindTransactionById()
    {
        string input = ReadRequired("Confirmed transaction ID: ");
        if (!Guid.TryParse(input, out Guid id))
        {
            Console.WriteLine("Invalid transaction ID");
            return;
        }
        var result = chain.FindTransactionById(id);
        if (result is null)
        {
            Console.WriteLine("Transaction not found in blockchain");
            return;
        }
        Console.WriteLine($"Block: {result.Value.BlockIndex}");
        display.DisplayTransactions([result.Value.Transaction]);
    }

    private void FindTransactionsByUser()
    {
        string user = ReadRequired("User: ");
        Console.WriteLine("1 - All operations");
        Console.WriteLine("2 - Sent only");
        Console.WriteLine("3 - Received only");
        TransactionDirection? direction = ReadRequired("Choose a filter: ") switch
        {
            "1" => TransactionDirection.All,
            "2" => TransactionDirection.Sent,
            "3" => TransactionDirection.Received,
            _ => null
        };
        if (direction is null)
        {
            Console.WriteLine("Invalid filter");
            return;
        }
        var results = chain.FindTransactionsByUser(user, direction.Value);
        Console.WriteLine($"CONFIRMED TRANSACTIONS | Matches: {results.Count}");
        if (results.Count == 0) Console.WriteLine("No transactions found");
        foreach (var result in results)
        {
            Console.WriteLine($"Block: {result.BlockIndex}");
            display.DisplayTransactions([result.Transaction]);
        }
    }

    private static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? throw new EndOfStreamException();
            if (input.Length > 0) return input;
            Console.WriteLine("Value cannot be empty");
        }
    }
}
