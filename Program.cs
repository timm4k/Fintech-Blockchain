using System.Text;
using FintechBlockchain;

Console.OutputEncoding = Encoding.UTF8;

var displayService = new BlockChainDisplayService();

const int initialDifficulty = 2;
const int blocksToMine = 8;
const int attackedBlockIndex = 2;
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
var blockchain = new BlockChain(initialDifficulty, displayService.DisplayMiningProgress);
for (int index = 1; index <= blocksToMine; index++)
{
    blockchain.AddBlock($"Mining sample block {index}");
}

displayService.PrintDifficultyHistory(blockchain);
int[] originalDifficulties = blockchain.Chain
    .Select(block => block.DifficultyAtMining)
    .ToArray();

Console.WriteLine("VALIDATION");
Console.WriteLine($"{"Stage",-18}{"Invalid block",-16}{"Reason",-25}{"IsValid"}");
displayService.DisplayValidation(blockchain, "Initial");

Block attackedBlock = blockchain.Chain[attackedBlockIndex];
attackedBlock.Data = "Forged event: transfer 4 BTC to an attacker";
displayService.DisplayValidation(blockchain, "Data changed");

var hashingService = new HashingService();
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
