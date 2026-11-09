using System.Text;
using FintechBlockchain;

Console.OutputEncoding = Encoding.UTF8;

const int initialDifficulty = 2;
const int blocksToMine = 8;
const int attackedBlockIndex = 2;
var displayService = new BlockChainDisplayService();
var hashingService = new HashingService();
string author = hashingService.ComputeHash("author:001");
Console.WriteLine("PART 1 | BLOCKCHAIN AND DIFFICULTY");
var blockchain = new BlockChain(initialDifficulty, displayService.DisplayMiningProgress);
for (int index = 1; index <= blocksToMine; index++)
{
    blockchain.AddBlock($"Mining sample block {index}", author);
}

int[] originalDifficulties = blockchain.Chain
    .Select(block => block.DifficultyAtMining)
    .ToArray();
int difficultyChanges = blockchain.Chain.Zip(blockchain.Chain.Skip(1))
    .Count(pair => pair.First.DifficultyAtMining != pair.Second.DifficultyAtMining);
Console.WriteLine($"BLOCKCHAIN | Blocks: {blockchain.Chain.Count} | Adjustment interval: {blockchain.AdjustmentInterval} | Difficulty changes: {difficultyChanges}");
displayService.PrintDifficultyHistory(blockchain, includeAuthor: false);

Console.WriteLine("PART 2 | TAMPERING DETECTION");
Console.WriteLine("VALIDATION");
Console.WriteLine($"{"Stage",-18}{"Invalid block",-16}{"Reason",-25}{"IsValid"}");
displayService.DisplayValidation(blockchain, "Initial");

Block attackedBlock = blockchain.Chain[attackedBlockIndex];
attackedBlock.Data = "Forged event: transfer 4 BTC to an attacker";
displayService.DisplayValidation(blockchain, "Data changed");

attackedBlock.Hash = hashingService.ComputeHash(attackedBlock);
displayService.DisplayValidation(blockchain, "Hash recalculated");

var miningService = new MiningService(hashingService);
miningService.MineBlock(attackedBlock, displayService.DisplayMiningProgress);
displayService.DisplayValidation(blockchain, "Block remined");
Console.WriteLine();

Console.WriteLine("PART 3 | CHAIN REPAIR");
RepairResult repair = blockchain.RepairChain(attackedBlockIndex);
int preservedDifficulties = blockchain.Chain.Zip(originalDifficulties)
    .Count(pair => pair.First.DifficultyAtMining == pair.Second);
displayService.DisplayRepairResult(repair, blockchain, preservedDifficulties);
displayService.PrintDifficultyHistory(blockchain, includeAuthor: false, startIndex: attackedBlockIndex);
displayService.DisplayDifficultyImpact(blockchain.Chain.Skip(attackedBlockIndex));

/*
Console.OutputEncoding = Encoding.UTF8;

int difficulty = 6;
int maxWorkers = Math.Max(2, Environment.ProcessorCount);
int workerCount = Math.Min(maxWorkers, 4);
if (args.Length > 2
    || (args.Length > 0 && (!int.TryParse(args[0], out difficulty) || difficulty is < 1 or > 64))
    || (args.Length > 1 && (!int.TryParse(args[1], out workerCount) || workerCount < 1 || workerCount > maxWorkers)))
{
    Console.Error.WriteLine($"Usage: dotnet run -- [difficulty: 1..64] [workers: 1..{maxWorkers}]");
    Environment.ExitCode = 1;
    return;
}

var hashingService = new HashingService();
var miningService = new MiningService(hashingService);
using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    DateTime timestamp = DateTime.UtcNow;
    string author = hashingService.ComputeHash("author:001");
    await miningService.MineBlockAsync(CreateBlock(1, timestamp, author), 1, cancellation.Token);
    await miningService.MineBlockAsync(CreateBlock(1, timestamp, author), workerCount, cancellation.Token);

    Block sequentialBlock = CreateBlock(difficulty, timestamp, author);
    Block parallelBlock = CreateBlock(difficulty, timestamp, author);
    Console.WriteLine("ASYNCHRONOUS AND MULTITHREADED MINING");
    Console.WriteLine($"Difficulty: {difficulty} | Logical processors: {Environment.ProcessorCount} | Parallel workers: {workerCount}");
    Console.WriteLine($"Same input: {hashingService.GetBlockData(sequentialBlock) == hashingService.GetBlockData(parallelBlock)}");
    Console.WriteLine(Console.IsInputRedirected
        ? "Ctrl+C: cancel"
        : "S: status | Q: cancel | Ctrl+C: cancel");
    Console.WriteLine();

    MiningResult sequential = await MiningConsole.WaitAsync(
        miningService.MineBlockAsync(sequentialBlock, 1, cancellation.Token),
        "Sequential", cancellation);
    MiningResult parallel = await MiningConsole.WaitAsync(
        miningService.MineBlockAsync(parallelBlock, workerCount, cancellation.Token),
        "Parallel", cancellation);
    MiningConsole.PrintComparison(hashingService, sequentialBlock, sequential, parallelBlock, parallel);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Mining canceled");
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

static Block CreateBlock(int difficulty, DateTime timestamp, string author)
{
    return new Block
    {
        Index = 1,
        TimeStamp = timestamp,
        Data = "Asynchronous mining comparison",
        Author = author,
        PreviousHash = "0",
        DifficultyAtMining = difficulty
    };
}
*/

/*
Console.OutputEncoding = Encoding.UTF8;

var displayService = new BlockChainDisplayService();
var hashingService = new HashingService();
string firstAuthor = hashingService.ComputeHash("author:001");
string secondAuthor = hashingService.ComputeHash("author:002");

var blockchain = new BlockChain(difficulty: 2);
blockchain.AddBlock("Document registered", firstAuthor);
blockchain.AddBlock("Document approved", secondAuthor);
displayService.DisplayChain(blockchain, includeMiningDetails: false);
Console.WriteLine($"IsValid: {blockchain.IsValid()}");
Console.WriteLine();

Block sampleBlock = blockchain.Chain[1];
string originalAuthor = sampleBlock.Author;
string originalHash = sampleBlock.Hash;
Console.WriteLine($"AUTHOR CHECK | Block {sampleBlock.Index}");
Console.WriteLine($"Original Author: {originalAuthor}");
Console.WriteLine($"Original Hash:   {originalHash}");

sampleBlock.Author = secondAuthor;
string changedHash = hashingService.ComputeHash(sampleBlock);
Console.WriteLine($"Changed Author:  {sampleBlock.Author}");
Console.WriteLine($"Computed Hash:   {changedHash}");
Console.WriteLine($"Stored Hash:     {sampleBlock.Hash}");
Console.WriteLine($"Hash changed: {changedHash != originalHash}");
Console.WriteLine($"IsValid (Author changed): {blockchain.IsValid()}");

sampleBlock.Author = originalAuthor;
Console.WriteLine($"Hash restored: {hashingService.ComputeHash(sampleBlock) == originalHash}");
Console.WriteLine($"IsValid (Author restored): {blockchain.IsValid()}");
*/

/*
Console.OutputEncoding = Encoding.UTF8;

var displayService = new BlockChainDisplayService();

const int initialDifficulty = 2;
const int blocksToMine = 8;
const int attackedBlockIndex = 2;
var hashingService = new HashingService();
string author = hashingService.ComputeHash("author:001");

var blockchain = new BlockChain(initialDifficulty, displayService.DisplayMiningProgress);
for (int index = 1; index <= blocksToMine; index++)
{
    blockchain.AddBlock($"Mining sample block {index}", author);
}

displayService.PrintDifficultyHistory(blockchain);
int[] originalDifficulties = blockchain.Chain
    .Select(block => block.DifficultyAtMining)
    .ToArray();

Console.WriteLine("VALIDATION");
Console.WriteLine($"{"Stage",-18}{"Invalid block",-16}{"Reason",-25}{"IsValid"}");
displayService.DisplayValidation(blockchain, "Initial");

Block attackedBlock = blockchain.Chain[attackedBlockIndex];
string originalAuthor = attackedBlock.Author;
string originalAuthorHash = hashingService.ComputeHash(attackedBlock);
string changedAuthor = hashingService.ComputeHash("author:002");
attackedBlock.Author = changedAuthor;
string changedAuthorHash = hashingService.ComputeHash(attackedBlock);
bool validWithChangedAuthor = blockchain.IsValid();
displayService.DisplayValidation(blockchain, "Author changed");
attackedBlock.Author = originalAuthor;
bool validWithRestoredAuthor = blockchain.IsValid();
displayService.DisplayValidation(blockchain, "Author restored");

attackedBlock.Data = "Forged event: transfer 4 BTC to an attacker";
displayService.DisplayValidation(blockchain, "Data changed");

attackedBlock.Hash = hashingService.ComputeHash(attackedBlock);
displayService.DisplayValidation(blockchain, "Hash recalculated");

var miningService = new MiningService(hashingService);
miningService.MineBlock(attackedBlock, displayService.DisplayMiningProgress);
displayService.DisplayValidation(blockchain, "Block remined");
Console.WriteLine();

RepairResult repair = blockchain.RepairChain(attackedBlockIndex);
int preservedDifficulties = blockchain.Chain.Zip(originalDifficulties)
    .Count(pair => pair.First.DifficultyAtMining == pair.Second);
displayService.DisplayRepairResult(repair, blockchain, preservedDifficulties);
displayService.DisplayDifficultyImpact(blockchain.Chain.Skip(attackedBlockIndex));

Console.WriteLine($"AUTHOR CHECK | Block {attackedBlockIndex}");
Console.WriteLine($"Original Author: {originalAuthor}");
Console.WriteLine($"Original Hash:   {originalAuthorHash}");
Console.WriteLine($"Changed Author:  {changedAuthor}");
Console.WriteLine($"Changed Hash:    {changedAuthorHash}");
Console.WriteLine($"Hash changed: {originalAuthorHash != changedAuthorHash}");
Console.WriteLine($"IsValid (Author changed): {validWithChangedAuthor}");
Console.WriteLine($"IsValid (Author restored): {validWithRestoredAuthor}");
*/

/*
string[] events =
[
    "User registration: Alice created an account",
    "Deposit: Alice added 5 BTC to her balance",
    "Purchase: Alice bought a laptop for 0.2 BTC",
    "Transfer: Alice sent 1.5 BTC to Bob",
    "Withdrawal: Bob withdrew 0.5 BTC"
];
*/

/*
var names = new[]
{
    (Text: "Таня", Alphabet: HashingService.UkrainianAlphabet),
    (Text: "Tanya", Alphabet: HashingService.LatinAlphabet)
};
var nameHashingService = new HashingService();
var nameMiningService = new MiningService(nameHashingService);
foreach (var name in names)
{
    Console.WriteLine($"MINING NAME | Prefix: {name.Text}");
    displayService.DisplayMiningProgress(0, TimeSpan.Zero);
    MiningResult nameResult = nameMiningService.MineText(name.Text, name.Alphabet, displayService.DisplayMiningProgress);
    displayService.DisplayMiningResult(nameResult);
}
*/

/*
Console.WriteLine($"SEARCH AND TAMPERING | Difficulty: {blockchain.Difficulty}");
DemonstrateTampering(blockchain, displayService);

while (true)
{
    Console.WriteLine("1 - Find block by index");
    Console.WriteLine("2 - Find events by text");
    Console.WriteLine("0 - Exit");
    Console.Write("Choose an option: ");
    string? choice = Console.ReadLine()?.Trim();
    Console.WriteLine();

    if (choice is null or "0")
    {
        break;
    }

    switch (choice)
    {
        case "1":
            SearchByIndex(blockchain, displayService);
            break;
        case "2":
            SearchByData(blockchain, displayService);
            break;
        default:
            Console.WriteLine("Enter 1, 2 or 0.");
            break;
    }

    Console.WriteLine();
}

static void SearchByIndex(BlockChain blockchain, BlockChainDisplayService displayService)
{
    Console.Write("Block index: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 0)
    {
        Console.WriteLine("Enter a non-negative integer index.");
        return;
    }

    Block? block = blockchain.FindBlockByIndex(index);
    if (block is null)
    {
        Console.WriteLine($"No block found with index {index}.");
        return;
    }

    Console.WriteLine();
    displayService.DisplayBlock(block);
}

static void SearchByData(BlockChain blockchain, BlockChainDisplayService displayService)
{
    Console.Write("Text in Data: ");
    string? text = Console.ReadLine();
    if (text is null)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine("Enter non-empty search text.");
        return;
    }

    IReadOnlyList<Block> matches = blockchain.FindBlocksByData(text.Trim());
    if (matches.Count == 0)
    {
        Console.WriteLine("No matching events found.");
        return;
    }

    Console.WriteLine($"Matches: {matches.Count}");
    Console.WriteLine();
    foreach (Block block in matches)
    {
        displayService.DisplayBlock(block);
    }
}

static void DemonstrateTampering(BlockChain blockchain, BlockChainDisplayService displayService)
{
    Block block = blockchain.Chain[3];
    string originalData = block.Data;

    Console.WriteLine($"TAMPERING DEMONSTRATION | Block {block.Index}");
    Console.WriteLine($"Original Data: {originalData}");
    block.Data = "Purchase: Alice bought a laptop for 4 BTC";
    displayService.DisplayBlock(block);
    Console.WriteLine($"Is Valid: {blockchain.IsChainValid()}");

    block.Data = originalData;
    Console.WriteLine($"Restored Data: {block.Data}");
    Console.WriteLine($"Is Valid after restoring original data: {blockchain.IsChainValid()}");
    Console.WriteLine();
}
*/
